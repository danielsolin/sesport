using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

using SESport.AI.Interfaces;

namespace SESport.AI.Providers;

public sealed class WebPageContentClient : IWebPageContentClient
{
   private const string BrowserUserAgent =
      "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
      "(KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36";
   internal const string CutoffMarker = "[CUTOFF]";
   internal const int MaxResponseCharacters = 12000;
   private static readonly TimeSpan BrowserNavigationTimeout =
      TimeSpan.FromSeconds(30);
   private static readonly TimeSpan BrowserLoadStateTimeout =
      TimeSpan.FromSeconds(30);
   private static readonly IReadOnlyDictionary<string, string>
      CountryNamesByCode = BuildCountryNamesByCode();
   private static readonly IReadOnlyDictionary<string, string>
      CountryNamesByThreeLetterCode = BuildCountryNamesByThreeLetterCode();
   private readonly HttpClient httpClient;
   private readonly ILogger<WebPageContentClient> logger;
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      browserPageFetcher;

   [ActivatorUtilitiesConstructor]
   public WebPageContentClient(HttpClient httpClient)
      : this(httpClient, null, null)
   {
   }

   public WebPageContentClient(
      HttpClient httpClient,
      Func<Uri, CancellationToken, Task<WebPageContent?>>? browserPageFetcher,
      ILogger<WebPageContentClient>? logger = null
   )
   {
      this.httpClient = httpClient;
      this.logger = logger ??
         Microsoft.Extensions.Logging.Abstractions.NullLogger<
            WebPageContentClient>.Instance;
      this.browserPageFetcher = browserPageFetcher ?? FetchBrowserPageAsync;
   }

   public async Task<WebPageContent?> FetchAsync(
      string url,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(url) ||
         !Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
      {
         return null;
      }

      using var request = new HttpRequestMessage(HttpMethod.Get, absoluteUrl);
      request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
      request.Headers.TryAddWithoutValidation("User-Agent", BrowserUserAgent);
      using var response = await httpClient.SendAsync(
         request,
         cancellationToken
      );

      if(IsPdfResponse(response, absoluteUrl))
      {
         return await FetchPdfAsync(response, absoluteUrl, cancellationToken);
      }

      try
      {
         return await browserPageFetcher(absoluteUrl, cancellationToken);
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(TimeoutException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright timed out for {Url}; falling back to HTML.",
            absoluteUrl
         );
         return await FetchHtmlFallbackAsync(
            response,
            absoluteUrl,
            cancellationToken
         );
      }
      catch(PlaywrightException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright failed for {Url}; falling back to HTML.",
            absoluteUrl
         );
         return await FetchHtmlFallbackAsync(
            response,
            absoluteUrl,
            cancellationToken
         );
      }
   }

   private static async Task<WebPageContent?> FetchBrowserPageAsync(
      Uri absoluteUrl,
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
               ViewportSize = new ViewportSize
               {
                  Width = 1440,
                  Height = 2400
               }
            }
         );

         await using var page = await context.NewPageAsync();
         await page.GotoAsync(
            absoluteUrl.ToString(),
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

         cancellationToken.ThrowIfCancellationRequested();

         var title = await page.TitleAsync();
         await page.EvaluateAsync(
            WebPageNormalizationScript.Build(),
            JsonSerializer.Serialize(CountryNamesByCode)
         );
         var visibleText = await page.Locator("body").InnerTextAsync();
         var normalizedText = NormalizeText(visibleText);

         return new WebPageContent(
            string.IsNullOrWhiteSpace(title) ? absoluteUrl.ToString() : title,
            absoluteUrl.ToString(),
            null,
            [],
            ApplyResponseCutoff(normalizedText),
            !string.IsNullOrWhiteSpace(visibleText),
            normalizedText
         );
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(TimeoutException)
      {
         return null;
      }
      catch(PlaywrightException)
      {
         return null;
      }
   }

   private static async Task<WebPageContent?> FetchPdfAsync(
      HttpResponseMessage response,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var pdfBytes = await response.Content.ReadAsByteArrayAsync(
            cancellationToken
         );

         if(pdfBytes.Length == 0)
         {
            return null;
         }

         using var pdfStream = new MemoryStream(pdfBytes);
         using var pdfDocument = PdfDocument.Open(pdfStream);
         var text = ExtractPdfText(pdfDocument);

         if(string.IsNullOrWhiteSpace(text))
         {
            return null;
         }

         var title = ExtractPdfTitle(pdfDocument, absoluteUrl);
         return new WebPageContent(
            title,
            absoluteUrl.ToString(),
            null,
            [],
            ApplyResponseCutoff(text),
            true,
            text
         );
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(Exception)
      {
         return null;
      }
   }

   private async Task<WebPageContent?> FetchHtmlFallbackAsync(
      HttpResponseMessage response,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var html = await response.Content.ReadAsStringAsync(
            cancellationToken
         );

         if(string.IsNullOrWhiteSpace(html))
         {
            logger.LogWarning(
               "HTML fallback had no body for {Url}.",
               absoluteUrl
            );
            return null;
         }

         var title = ExtractHtmlTitle(html);
         var text = ExtractHtmlText(html);

         if(string.IsNullOrWhiteSpace(text))
         {
            logger.LogWarning(
               "HTML fallback produced no text for {Url}.",
               absoluteUrl
            );
            return null;
         }

         logger.LogInformation(
            "HTML fallback used for {Url}.",
            absoluteUrl
         );

         return new WebPageContent(
            title ?? absoluteUrl.ToString(),
            absoluteUrl.ToString(),
            null,
            [],
            ApplyResponseCutoff(text),
            true,
            text
         );
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(Exception)
      {
         return null;
      }
   }

   private static string? ExtractHtmlTitle(string html)
   {
      var match = Regex.Match(
         html,
         @"<title[^>]*>(.*?)</title>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );

      if(!match.Success)
      {
         return null;
      }

      return WebUtility.HtmlDecode(
         StripTags(match.Groups[1].Value).Trim()
      );
   }

   private static string ExtractHtmlText(string html)
   {
      var cleanedHtml = Regex.Replace(
         html,
         @"<script\b[^>]*>.*?</script>",
         " ",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );
      cleanedHtml = Regex.Replace(
         cleanedHtml,
         @"<style\b[^>]*>.*?</style>",
         " ",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );
      cleanedHtml = Regex.Replace(
         cleanedHtml,
         @"<noscript\b[^>]*>.*?</noscript>",
         " ",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );
      cleanedHtml = Regex.Replace(
         cleanedHtml,
         @"<[^>]+>",
         " "
      );

      return NormalizeText(WebUtility.HtmlDecode(cleanedHtml));
   }

   private static string StripTags(string value)
   {
      return Regex.Replace(value, @"<[^>]+>", " ");
   }

   private static bool IsPdfResponse(
      HttpResponseMessage response,
      Uri absoluteUrl
   )
   {
      var contentType = response.Content.Headers.ContentType?.MediaType;

      if(string.Equals(
         contentType,
         "application/pdf",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return true;
      }

      if(string.Equals(
         contentType,
         "application/x-pdf",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return true;
      }

      return absoluteUrl.AbsolutePath.EndsWith(
         ".pdf",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static string ExtractPdfText(PdfDocument pdfDocument)
   {
      var pages = pdfDocument
         .GetPages()
         .Select(page => ContentOrderTextExtractor.GetText(page, true))
         .Where(text => !string.IsNullOrWhiteSpace(text))
         .Select(text => text.Trim());

      return string.Join(Environment.NewLine, pages);
   }

   private static string ExtractPdfTitle(
      PdfDocument pdfDocument,
      Uri absoluteUrl
   )
   {
      var title = pdfDocument.Information.Title?.Trim();

      if(!string.IsNullOrWhiteSpace(title))
      {
         return title;
      }

      var fileName = Path.GetFileNameWithoutExtension(
         absoluteUrl.AbsolutePath
      );

      if(!string.IsNullOrWhiteSpace(fileName))
      {
         return fileName;
      }

      return absoluteUrl.ToString();
   }

   internal static string? GetCountryDisplayName(string? countryCode)
   {
      if(string.IsNullOrWhiteSpace(countryCode))
      {
         return null;
      }

      var normalizedCode = countryCode.Trim().ToUpperInvariant();

      if(normalizedCode.Length == 3 &&
         CountryNamesByThreeLetterCode is
            { } threeLetterCountryNames &&
         threeLetterCountryNames.TryGetValue(
            normalizedCode,
            out var threeLetterDisplayName
         ))
      {
         return threeLetterDisplayName;
      }

      try
      {
         return new RegionInfo(normalizedCode)
            .EnglishName;
      }
      catch(ArgumentException)
      {
         return null;
      }
   }

   private static IReadOnlyDictionary<string, string> BuildCountryNamesByCode()
   {
      var countryNames = new Dictionary<string, string>(
         StringComparer.OrdinalIgnoreCase
      );

      foreach(var culture in CultureInfo.GetCultures(
         CultureTypes.SpecificCultures
      ))
      {
         RegionInfo? region;

         try
         {
            region = new RegionInfo(culture.Name);
         }
         catch(ArgumentException)
         {
            continue;
         }

         var code = region.TwoLetterISORegionName;

         if(countryNames.ContainsKey(code))
         {
            continue;
         }

         countryNames[code] = region.EnglishName;
         countryNames[region.ThreeLetterISORegionName] =
            region.EnglishName;
      }

      return countryNames;
   }

   private static IReadOnlyDictionary<string, string>
      BuildCountryNamesByThreeLetterCode()
   {
      var countryNames = new Dictionary<string, string>(
         StringComparer.OrdinalIgnoreCase
      );

      foreach(var culture in CultureInfo.GetCultures(
         CultureTypes.SpecificCultures
      ))
      {
         RegionInfo? region;

         try
         {
            region = new RegionInfo(culture.Name);
         }
         catch(ArgumentException)
         {
            continue;
         }

         var code = region.ThreeLetterISORegionName;

         if(countryNames.ContainsKey(code))
         {
            continue;
         }

         countryNames[code] = region.EnglishName;
      }

      return countryNames;
   }

   private static string NormalizeText(string? text)
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         return string.Empty;
      }

      return text.Replace("\r", "\n", StringComparison.Ordinal).Trim();
   }

   internal static string ApplyResponseCutoff(string text)
   {
      if(string.IsNullOrWhiteSpace(text) ||
         text.Length <= MaxResponseCharacters)
      {
         return text;
      }

      var cutoffLength = MaxResponseCharacters - CutoffMarker.Length;

      if(cutoffLength <= 0)
      {
         return CutoffMarker;
      }

      return text[..cutoffLength].TrimEnd() + CutoffMarker;
   }
}
