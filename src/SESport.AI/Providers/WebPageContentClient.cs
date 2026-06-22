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
   private const string BrowserUserAgentFallback =
      "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
      "(KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36";
   private const string BrowserAcceptHeader =
      "text/html,application/xhtml+xml,application/xml;q=0.9," +
      "image/avif,image/webp,*/*;q=0.8";
   private const string BrowserAcceptLanguageHeader = "en-US,en;q=0.9";
   private const string BrowserLocale = "en-US";
   private const string BrowserPlatform = "Linux";
   private const string AutomationEvasionScript = """
      Object.defineProperty(navigator, 'webdriver', {
         get: () => undefined
      });
      Object.defineProperty(navigator, 'languages', {
         get: () => ['en-US', 'en']
      });
      Object.defineProperty(navigator, 'platform', {
         get: () => 'Linux x86_64'
      });
      Object.defineProperty(navigator, 'vendor', {
         get: () => 'Google Inc.'
      });
      """;
   internal const string CutoffMarker = "[CUTOFF]";
   internal const int MaxResponseCharacters = 20000;
   private static readonly TimeSpan BrowserNavigationTimeout =
      TimeSpan.FromSeconds(30);
   private static readonly TimeSpan BrowserLoadStateTimeout =
      TimeSpan.FromSeconds(30);
   private static readonly IReadOnlyDictionary<string, string>
      CountryNamesByCode = BuildCountryNamesByCode();
   private static readonly IReadOnlyDictionary<string, string>
      CountryNamesByThreeLetterCode = BuildCountryNamesByThreeLetterCode();
   private static readonly Lazy<Task<string>> BrowserUserAgentTask =
      new(BuildBrowserUserAgentAsync);
   private readonly HttpClient httpClient;
   private readonly ILogger<WebPageContentClient> logger;
   private readonly Func<Task<string>> browserUserAgentFetcher;
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      browserPageFetcher;

   [ActivatorUtilitiesConstructor]
   public WebPageContentClient(HttpClient httpClient)
      : this(httpClient, null, null, null)
   {
   }

   public WebPageContentClient(
      HttpClient httpClient,
      Func<Uri, CancellationToken, Task<WebPageContent?>>? browserPageFetcher,
      ILogger<WebPageContentClient>? logger = null,
      Func<Task<string>>? browserUserAgentFetcher = null
   )
   {
      this.httpClient = httpClient;
      this.logger = logger ??
         Microsoft.Extensions.Logging.Abstractions.NullLogger<
            WebPageContentClient>.Instance;
      this.browserUserAgentFetcher = browserUserAgentFetcher ??
         GetBrowserUserAgentAsync;
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

      var browserUserAgent = await this.browserUserAgentFetcher();
      var browserLikeHeaders = BuildBrowserLikeHeaders(browserUserAgent);
      using var request = new HttpRequestMessage(HttpMethod.Get, absoluteUrl);
      request.Headers.Accept.ParseAdd(BrowserAcceptHeader);
      foreach(var header in browserLikeHeaders)
      {
         request.Headers.TryAddWithoutValidation(
            header.Key,
            header.Value
         );
      }

      request.Headers.TryAddWithoutValidation("User-Agent", browserUserAgent);
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

   private async Task<WebPageContent?> FetchBrowserPageAsync(
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var absoluteUrlString = absoluteUrl.ToString();
         var browserUserAgent = await this.browserUserAgentFetcher();
         var browserLikeHeaders = BuildBrowserLikeHeaders(browserUserAgent);
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
               UserAgent = browserUserAgent,
               Locale = BrowserLocale,
               ExtraHTTPHeaders = browserLikeHeaders,
               ViewportSize = new ViewportSize
               {
                  Width = 1440,
                  Height = 2400
               }
            }
         );

         await context.AddInitScriptAsync(AutomationEvasionScript);

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
            string.IsNullOrWhiteSpace(title) ? absoluteUrlString : title,
            absoluteUrlString,
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
         var absoluteUrlString = absoluteUrl.ToString();
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
            absoluteUrlString,
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
         var absoluteUrlString = absoluteUrl.ToString();
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
            title ?? absoluteUrlString,
            absoluteUrlString,
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

   internal static string BuildBrowserUserAgent(string browserVersion)
   {
      var majorVersionMatch = Regex.Match(
         browserVersion,
         @"\b(\d+)",
         RegexOptions.CultureInvariant
      );

      if(!majorVersionMatch.Success ||
         !int.TryParse(
            majorVersionMatch.Groups[1].Value,
            out var majorVersion
         ) ||
         majorVersion <= 0)
      {
         return BrowserUserAgentFallback;
      }

      return
         "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
         $"(KHTML, like Gecko) Chrome/{majorVersion}.0.0.0 Safari/537.36";
   }

   private static async Task<string> GetBrowserUserAgentAsync()
   {
      try
      {
         return await BrowserUserAgentTask.Value;
      }
      catch
      {
         return BrowserUserAgentFallback;
      }
   }

   private static IReadOnlyDictionary<string, string>
      BuildBrowserLikeHeaders(string browserUserAgent)
   {
      return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      {
         ["Accept"] = BrowserAcceptHeader,
         ["Accept-Language"] = BrowserAcceptLanguageHeader,
         ["Upgrade-Insecure-Requests"] = "1",
         ["Sec-CH-UA"] = BuildSecChUaHeader(browserUserAgent),
         ["Sec-CH-UA-Mobile"] = "?0",
         ["Sec-CH-UA-Platform"] = $"\"{BrowserPlatform}\""
      };
   }

   private static string BuildSecChUaHeader(string browserUserAgent)
   {
      var majorVersionMatch = Regex.Match(
         browserUserAgent,
         @"Chrome/(\d+)",
         RegexOptions.CultureInvariant
      );

      var majorVersion = majorVersionMatch.Success &&
         int.TryParse(
            majorVersionMatch.Groups[1].Value,
            out var parsedMajorVersion
         )
         ? parsedMajorVersion
         : 125;

      return
         $"\"Chromium\";v=\"{majorVersion}\", " +
         $"\"Not A(Brand\";v=\"24\", \"Google Chrome\";v=\"{majorVersion}\"";
   }

   private static async Task<string> BuildBrowserUserAgentAsync()
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

         return BuildBrowserUserAgent(browser.Version);
      }
      catch
      {
         return BrowserUserAgentFallback;
      }
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
