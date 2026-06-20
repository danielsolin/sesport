using System.Net;

using Microsoft.Playwright;

using SESport.AI.Interfaces;

namespace SESport.AI.Providers;

public sealed class GoogleWebSearchClient : IWebSearchClient
{
   private const string GoogleSearchBaseUrl =
      "https://www.google.com/search";

   private const string BrowserUserAgent =
      "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
      "(KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36";

   private static readonly string[] DeniedHostSuffixes =
   [
      "instagram.com",
      "www.instagram.com",
      "facebook.com",
      "www.facebook.com",
      "x.com",
      "www.x.com",
      "twitter.com",
      "www.twitter.com",
      "tiktok.com",
      "www.tiktok.com",
      "youtube.com",
      "www.youtube.com",
      "youtu.be",
      "www.youtu.be",
      "threads.net",
      "www.threads.net"
   ];

   private static readonly TimeSpan BrowserNavigationTimeout =
      TimeSpan.FromSeconds(30);

   private static readonly TimeSpan BrowserLoadStateTimeout =
      TimeSpan.FromSeconds(30);

   private readonly Func<Uri, int, CancellationToken,
      Task<GoogleSearchAttempt>> searchFetcher;

   public GoogleWebSearchClient(HttpClient httpClient)
      : this(httpClient, null)
   {
   }

   internal GoogleWebSearchClient(
      HttpClient httpClient,
      Func<Uri, int, CancellationToken,
         Task<GoogleSearchAttempt>>? searchFetcher
   )
   {
      _ = httpClient;
      this.searchFetcher = searchFetcher ?? FetchGoogleResultsAsync;
   }

   public async Task<WebSearchResponse> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(query))
      {
         return new WebSearchResponse([]);
      }

      var searchUri = BuildGoogleSearchUri(query, maxResults);
      var attempt = await searchFetcher(
         searchUri,
         maxResults,
         cancellationToken
      );
      return new WebSearchResponse(
         attempt.Results,
         "Google",
         attempt.FailureMessage
      );
   }

   private static Uri BuildGoogleSearchUri(
      string query,
      int maxResults
   )
   {
      var queryString = string.Join(
         "&",
         new Dictionary<string, string>
         {
            ["q"] = query,
            ["hl"] = "en",
            ["gl"] = "us",
            ["pws"] = "0",
            ["num"] = Math.Clamp(maxResults, 1, 20).ToString()
         }.Select(pair =>
            $"{WebUtility.UrlEncode(pair.Key)}=" +
            $"{WebUtility.UrlEncode(pair.Value)}"
         )
      );

      return new Uri($"{GoogleSearchBaseUrl}?{queryString}");
   }

   private static async Task<GoogleSearchAttempt>
      FetchGoogleResultsAsync(
         Uri searchUri,
         int maxResults,
         CancellationToken cancellationToken
      )
   {
      try
      {
         using var playwright = await Playwright.CreateAsync();
         await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
               Headless = true
            }
         );

         await using var context = await browser.NewContextAsync(
            new BrowserNewContextOptions
            {
               UserAgent = BrowserUserAgent,
               Locale = "en-US",
               ViewportSize = new ViewportSize
               {
                  Width = 1440,
                  Height = 2400
               }
            }
         );

         await using var page = await context.NewPageAsync();
         await page.GotoAsync(
            searchUri.ToString(),
            new PageGotoOptions
            {
               WaitUntil = WaitUntilState.DOMContentLoaded,
               Timeout = (float)BrowserNavigationTimeout.TotalMilliseconds
            }
         );

         try
         {
            await page.WaitForLoadStateAsync(
               LoadState.NetworkIdle,
               new PageWaitForLoadStateOptions
               {
                  Timeout = (float)BrowserLoadStateTimeout.TotalMilliseconds
               }
            );
         }
         catch(PlaywrightException)
         {
         }
         catch(TimeoutException)
         {
         }

         try
         {
            await page.WaitForSelectorAsync(
               "a:has(h3)",
               new PageWaitForSelectorOptions
               {
                  Timeout = (float)BrowserLoadStateTimeout.TotalMilliseconds
               }
            );
         }
         catch(PlaywrightException)
         {
         }
         catch(TimeoutException)
         {
         }

         cancellationToken.ThrowIfCancellationRequested();

         var results = await ParseGoogleResultsAsync(
            page,
            maxResults,
            cancellationToken
         );
         return new GoogleSearchAttempt(results);
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(PlaywrightException exception)
      {
         return new GoogleSearchAttempt([], exception.Message);
      }
   }

   private static async Task<IReadOnlyList<WebSearchResult>>
      ParseGoogleResultsAsync(
         IPage page,
         int maxResults,
         CancellationToken cancellationToken
      )
   {
      var items = new List<WebSearchResult>();
      var anchors = page.Locator("a:has(h3)");
      var count = await anchors.CountAsync();

      for(var i = 0; i < count; i++)
      {
         cancellationToken.ThrowIfCancellationRequested();

         var anchor = anchors.Nth(i);
         var title = NormalizeText(await anchor.Locator("h3").InnerTextAsync());
         var href = await anchor.GetAttributeAsync("href");
         var url = ExtractGoogleResultUrl(href);

         if(string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(url))
         {
            continue;
         }

         if(IsDeniedDomain(url))
         {
            continue;
         }

         items.Add(
            new WebSearchResult(
               title,
               url,
               null,
               null
            )
         );

         if(items.Count >= Math.Clamp(maxResults, 1, 20))
         {
            break;
         }
      }

      return items;
   }

   private static string? ExtractGoogleResultUrl(string? href)
   {
      if(string.IsNullOrWhiteSpace(href))
      {
         return null;
      }

      if(!Uri.TryCreate(href, UriKind.Absolute, out var absoluteHref) &&
         !Uri.TryCreate(
            new Uri("https://www.google.com"),
            href,
            out absoluteHref
         ))
      {
         return null;
      }

      var query = absoluteHref.Query;
      if(string.IsNullOrWhiteSpace(query))
      {
         return absoluteHref.ToString();
      }

      var targetUrl =
         ReadQueryParameter(query, "url") ??
         ReadQueryParameter(query, "q");

      if(string.IsNullOrWhiteSpace(targetUrl))
      {
         return absoluteHref.ToString();
      }

      return WebUtility.UrlDecode(targetUrl);
   }

   private static string? ReadQueryParameter(
      string query,
      string key
   )
   {
      foreach(var pair in query.TrimStart('?').Split(
         '&',
         StringSplitOptions.RemoveEmptyEntries
      ))
      {
         var separatorIndex = pair.IndexOf('=');
         var pairKey = separatorIndex >= 0
            ? pair[..separatorIndex]
            : pair;

         if(!string.Equals(
            WebUtility.UrlDecode(pairKey),
            key,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            continue;
         }

         return separatorIndex >= 0
            ? pair[(separatorIndex + 1)..]
            : string.Empty;
      }

      return null;
   }

   private static bool IsDeniedDomain(string url)
   {
      if(!Uri.TryCreate(url, UriKind.Absolute, out var uri))
      {
         return false;
      }

      var host = uri.Host;

      foreach(var deniedHostSuffix in DeniedHostSuffixes)
      {
         if(host.EndsWith(
            deniedHostSuffix,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            return true;
         }
      }

      return false;
   }

   private static string NormalizeText(string? text)
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         return string.Empty;
      }

      return text.Replace("\r", "\n", StringComparison.Ordinal).Trim();
   }

   internal sealed record GoogleSearchAttempt(
      IReadOnlyList<WebSearchResult> Results,
      string? FailureMessage = null
   );
}
