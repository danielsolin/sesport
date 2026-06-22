using System.Diagnostics;
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
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      curlPageFetcher;

   [ActivatorUtilitiesConstructor]
   public WebPageContentClient(HttpClient httpClient)
      : this(httpClient, null, null, null)
   {
   }

   public WebPageContentClient(
      HttpClient httpClient,
      Func<Uri, CancellationToken, Task<WebPageContent?>>? browserPageFetcher,
      ILogger<WebPageContentClient>? logger = null,
      Func<Task<string>>? browserUserAgentFetcher = null,
      Func<Uri, CancellationToken, Task<WebPageContent?>>? curlPageFetcher =
         null
   )
   {
      this.httpClient = httpClient;
      this.logger = logger ??
         Microsoft.Extensions.Logging.Abstractions.NullLogger<
            WebPageContentClient>.Instance;
      this.browserUserAgentFetcher = browserUserAgentFetcher ??
         GetBrowserUserAgentAsync;
      this.browserPageFetcher = browserPageFetcher ?? FetchBrowserPageAsync;
      this.curlPageFetcher = curlPageFetcher ?? FetchCurlPageAsync;
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
      catch(WebPageFetchException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright failed for {Url}; falling back to HTML.",
            absoluteUrl
         );
         return await FetchHtmlFallbackAsync(
            response,
            absoluteUrl,
            cancellationToken,
            exception.ErrorKind
         );
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
            cancellationToken,
            WebPageFetchErrorKind.Timeout
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
            cancellationToken,
            WebPageFetchErrorKind.BrowserBlocked
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
         throw new WebPageFetchException(
            WebPageFetchErrorKind.Timeout,
            "Playwright timed out."
         );
      }
      catch(PlaywrightException)
      {
         throw new WebPageFetchException(
            WebPageFetchErrorKind.BrowserBlocked,
            "Playwright failed."
         );
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
            return BuildFailureContent(
               absoluteUrl,
               null,
               null,
               "PDF response had no body."
            );
         }

         using var pdfStream = new MemoryStream(pdfBytes);
         using var pdfDocument = PdfDocument.Open(pdfStream);
         var text = ExtractPdfText(pdfDocument);

         if(string.IsNullOrWhiteSpace(text))
         {
            return BuildFailureContent(
               absoluteUrl,
               title: ExtractPdfTitle(pdfDocument, absoluteUrl),
               fetchErrorKind: null,
               fetchErrorMessage: "PDF response produced no text."
            );
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
         return BuildFailureContent(
            absoluteUrl,
            null,
            null,
            "Unable to extract PDF response."
         );
      }
   }

   private async Task<WebPageContent?> FetchHtmlFallbackAsync(
      HttpResponseMessage response,
      Uri absoluteUrl,
      CancellationToken cancellationToken,
      WebPageFetchErrorKind? browserFailureKind = null
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
            return await TryCurlFallbackAsync(
               absoluteUrl,
               browserFailureKind,
               "HTML fallback had no body.",
               cancellationToken
            );
         }

         var title = ExtractHtmlTitle(html);
         var text = ExtractHtmlTextWithEmbeddedState(html);

         if(string.IsNullOrWhiteSpace(text))
         {
            logger.LogWarning(
               "HTML fallback produced no text for {Url}.",
               absoluteUrl
            );
            if(IsBlockedPage(title, text))
            {
               return await TryCurlFallbackAsync(
                  absoluteUrl,
                  browserFailureKind,
                  "HTML fallback produced no text.",
                  cancellationToken
               );
            }

            return BuildFailureContent(
               absoluteUrl,
               title ?? absoluteUrlString,
               browserFailureKind,
               "HTML fallback produced no text."
            );
         }

         logger.LogInformation(
            "HTML fallback used for {Url}.",
            absoluteUrl
         );

         if(IsBlockedPage(title, text))
         {
            return await TryCurlFallbackAsync(
               absoluteUrl,
               browserFailureKind,
               "HTML fallback was blocked.",
               cancellationToken
            );
         }

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
         return BuildFailureContent(
            absoluteUrl,
            null,
            browserFailureKind,
            "Unable to extract HTML fallback."
         );
      }
   }

   private static WebPageContent BuildFailureContent(
      Uri absoluteUrl,
      string? title,
      WebPageFetchErrorKind? fetchErrorKind,
      string fetchErrorMessage
   )
   {
      var absoluteUrlString = absoluteUrl.ToString();

      return new WebPageContent(
         string.IsNullOrWhiteSpace(title) ? absoluteUrlString : title,
         absoluteUrlString,
         null,
         [],
         string.Empty,
         false,
         string.Empty,
         fetchErrorMessage,
         fetchErrorKind
      );
   }

   private async Task<WebPageContent?> TryCurlFallbackAsync(
      Uri absoluteUrl,
      WebPageFetchErrorKind? browserFailureKind,
      string fallbackFailureMessage,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var curlContent = await this.curlPageFetcher(
            absoluteUrl,
            cancellationToken
         );

         if(curlContent is not null)
         {
            return curlContent;
         }
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(Exception exception)
      {
         logger.LogWarning(
            exception,
            "Curl fallback failed for {Url}.",
            absoluteUrl
         );
      }

      return BuildFailureContent(
         absoluteUrl,
         null,
         browserFailureKind,
         fallbackFailureMessage
      );
   }

   private static bool IsBlockedPage(string? title, string text)
   {
      var combinedText = NormalizeText($"{title} {text}");

      return combinedText.Contains(
         "access denied",
         StringComparison.OrdinalIgnoreCase
      ) ||
      combinedText.Contains(
         "you do not have permission to access",
         StringComparison.OrdinalIgnoreCase
      ) ||
      combinedText.Contains(
         "you don't have permission to access",
         StringComparison.OrdinalIgnoreCase
      ) ||
      combinedText.Contains(
         "errors edgesuite net",
         StringComparison.OrdinalIgnoreCase
      ) ||
      combinedText.Contains(
         "reference",
         StringComparison.OrdinalIgnoreCase
      );
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

   private static string ExtractHtmlTextWithEmbeddedState(string html)
   {
      var tableText = ExtractStructuredTableText(html);
      var embeddedText = ExtractEmbeddedStateText(html);
      var visibleText = ExtractHtmlText(html);

      if(string.IsNullOrWhiteSpace(tableText))
      {
         tableText = string.Empty;
      }

      if(string.IsNullOrWhiteSpace(embeddedText))
      {
         return NormalizeText(
            string.Join(
               Environment.NewLine,
               new[]
               {
                  tableText,
                  visibleText
               }.Where(text => !string.IsNullOrWhiteSpace(text))
            )
         );
      }

      if(string.IsNullOrWhiteSpace(visibleText))
      {
         return NormalizeText(
            string.Join(
               Environment.NewLine,
               new[]
               {
                  tableText,
                  embeddedText
               }.Where(text => !string.IsNullOrWhiteSpace(text))
            )
         );
      }

      return NormalizeText(
         string.Join(
            Environment.NewLine,
            new[]
            {
               tableText,
               embeddedText,
               visibleText
            }.Where(text => !string.IsNullOrWhiteSpace(text))
         )
      );
   }

   private static string ExtractHtmlText(string html)
   {
      html = Regex.Replace(
         html,
         @"<header\b[^>]*>.*?</header>",
         " ",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );
      html = Regex.Replace(
         html,
         @"<nav\b[^>]*>.*?</nav>",
         " ",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );
      html = Regex.Replace(
         html,
         @"<footer\b[^>]*>.*?</footer>",
         " ",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );
      html = Regex.Replace(
         html,
         @"<aside\b[^>]*>.*?</aside>",
         " ",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );
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

   private static string ExtractEmbeddedStateText(string html)
   {
      var texts = new List<string>();
      var seenTexts = new HashSet<string>(StringComparer.Ordinal);
      var scriptMatches = Regex.Matches(
         html,
         @"<script\b([^>]*)>(.*?)</script>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );

      foreach(Match match in scriptMatches)
      {
         if(!TryExtractStructuredJsonText(
            match.Groups[1].Value,
            match.Groups[2].Value,
            out var embeddedText
         ))
         {
            continue;
         }

         foreach(var line in embeddedText.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries
         ))
         {
            var normalizedLine = NormalizeText(line);

            if(string.IsNullOrWhiteSpace(normalizedLine) ||
               !seenTexts.Add(normalizedLine))
            {
               continue;
            }

            texts.Add(normalizedLine);
         }
      }

      return NormalizeText(string.Join(Environment.NewLine, texts));
   }

   private static string ExtractStructuredTableText(string html)
   {
      var preferredTexts = new List<string>();
      var otherTexts = new List<string>();
      var seenTexts = new HashSet<string>(StringComparer.Ordinal);
      var tableMatches = Regex.Matches(
         html,
         @"<(?<tag>[a-zA-Z0-9:-]+)(?<attrs>[^>]*)\brole=""(?<role>cell|gridcell|rowheader|columnheader)""[^>]*>(?<content>.*?)</\k<tag>>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );

      foreach(Match match in tableMatches)
      {
         var normalizedText = NormalizeText(
            WebUtility.HtmlDecode(
               StripTags(match.Groups["content"].Value)
            )
         );

         if(!ShouldCaptureEmbeddedValue(null, normalizedText) ||
            !seenTexts.Add(normalizedText))
         {
            continue;
         }

         if(IsLikelyReadableStructuredPhrase(normalizedText))
         {
            preferredTexts.Add(normalizedText);
         }
         else
         {
            otherTexts.Add(normalizedText);
         }
      }

      var texts = preferredTexts.Count > 0 ? preferredTexts : otherTexts;
      return NormalizeText(string.Join(Environment.NewLine, texts));
   }

   private static bool TryExtractStructuredJsonText(
      string scriptAttributes,
      string scriptContent,
      out string text
   )
   {
      text = string.Empty;
      var normalizedContent = WebUtility.HtmlDecode(scriptContent).Trim();

      if(string.IsNullOrWhiteSpace(normalizedContent))
      {
         return false;
      }

      if(!scriptAttributes.Contains("application/json",
            StringComparison.OrdinalIgnoreCase) &&
         !scriptAttributes.Contains("application/ld+json",
            StringComparison.OrdinalIgnoreCase) &&
         !LooksLikeStructuredScript(normalizedContent))
      {
         return false;
      }

      if(!TryParseJsonDocument(normalizedContent, out var document))
      {
         return false;
      }

      if(document is null)
      {
         return false;
      }

      using(document)
      {
         var values = new List<string>();
         var seenValues = new HashSet<string>(StringComparer.Ordinal);
         var rootElement = document.RootElement;
         CollectEmbeddedText(
            rootElement,
            null,
            values,
            seenValues
         );

         text = NormalizeText(string.Join(Environment.NewLine, values));
         return !string.IsNullOrWhiteSpace(text);
      }
   }

   private static bool LooksLikeStructuredScript(string scriptContent)
   {
      if(scriptContent.StartsWith("{", StringComparison.Ordinal) ||
         scriptContent.StartsWith("[", StringComparison.Ordinal) ||
         scriptContent.Contains("__INITIAL_STATE__",
            StringComparison.Ordinal) ||
         scriptContent.Contains("__NEXT_DATA__", StringComparison.Ordinal) ||
         scriptContent.Contains("prerender-data-cache",
            StringComparison.Ordinal))
      {
         return true;
      }

      return scriptContent.Contains("=", StringComparison.Ordinal) &&
         (scriptContent.Contains("{", StringComparison.Ordinal) ||
          scriptContent.Contains("[", StringComparison.Ordinal));
   }

   private static bool TryParseJsonDocument(
      string content,
      out JsonDocument? document
   )
   {
      document = null;

      if(TryParseJsonDocumentCore(content, out document))
      {
         return true;
      }

      if(!TryExtractJsonFragment(content, out var jsonFragment))
      {
         return false;
      }

      return TryParseJsonDocumentCore(jsonFragment, out document);
   }

   private static bool TryParseJsonDocumentCore(
      string content,
      out JsonDocument? document
   )
   {
      document = null;

      try
      {
         document = JsonDocument.Parse(
            content.Trim().TrimEnd(';'),
            new JsonDocumentOptions
            {
               AllowTrailingCommas = true
            }
         );

         if(document.RootElement.ValueKind == JsonValueKind.String)
         {
            var embeddedJson = document.RootElement.GetString();

            if(!string.IsNullOrWhiteSpace(embeddedJson) &&
               TryParseJsonDocumentCore(embeddedJson, out var nestedDocument))
            {
               document.Dispose();
               document = nestedDocument;
            }
         }

         return true;
      }
      catch(JsonException)
      {
         document?.Dispose();
         document = null;
         return false;
      }
   }

   private static bool TryExtractJsonFragment(
      string content,
      out string jsonFragment
   )
   {
      jsonFragment = string.Empty;

      var startIndex = content.IndexOfAny(['{', '[']);

      if(startIndex < 0)
      {
         return false;
      }

      var endIndex = Math.Max(
         content.LastIndexOf('}'),
         content.LastIndexOf(']')
      );

      if(endIndex <= startIndex)
      {
         return false;
      }

      jsonFragment = content[startIndex..(endIndex + 1)];
      return true;
   }

   private static void CollectEmbeddedText(
      JsonElement element,
      string? propertyName,
      ICollection<string> texts,
      ISet<string> seenTexts
   )
   {
      switch(element.ValueKind)
      {
         case JsonValueKind.Object:
            foreach(var property in element.EnumerateObject())
            {
               CollectEmbeddedText(
                  property.Value,
                  property.Name,
                  texts,
                  seenTexts
               );
            }

            break;
         case JsonValueKind.Array:
            foreach(var item in element.EnumerateArray())
            {
               CollectEmbeddedText(item, propertyName, texts, seenTexts);
            }

            break;
         case JsonValueKind.String:
            AddEmbeddedValue(
               propertyName,
               element.GetString(),
               texts,
               seenTexts
            );

            break;
         case JsonValueKind.Number:
         case JsonValueKind.True:
         case JsonValueKind.False:
            AddEmbeddedValue(
               propertyName,
               element.ToString(),
               texts,
               seenTexts
            );

            break;
      }
   }

   private static void AddEmbeddedValue(
      string? propertyName,
      string? value,
      ICollection<string> texts,
      ISet<string> seenTexts
   )
   {
      if(!ShouldCaptureEmbeddedValue(propertyName, value))
      {
         return;
      }

      var normalizedValue = NormalizeText(value);

      if(string.IsNullOrWhiteSpace(normalizedValue) ||
         !seenTexts.Add(normalizedValue))
      {
         return;
      }

      texts.Add(normalizedValue);
   }

   private static bool ShouldCaptureEmbeddedValue(
      string? propertyName,
      string? value
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return false;
      }

      var normalizedValue = NormalizeText(value);

      if(normalizedValue.Length < 2 || normalizedValue.Length > 160)
      {
         return false;
      }

      if(IsLikelyMachineValue(normalizedValue))
      {
         return false;
      }

      if(IsLikelyDisplayProperty(propertyName))
      {
         return true;
      }

      return IsLikelyHumanReadable(normalizedValue);
   }

   private static bool IsLikelyDisplayProperty(string? propertyName)
   {
      if(string.IsNullOrWhiteSpace(propertyName))
      {
         return false;
      }

      var normalizedPropertyName = propertyName.Trim().ToLowerInvariant();

      return normalizedPropertyName.EndsWith("name", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("title", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("label", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("text", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("description",
            StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("caption", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("headline", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("standfirst",
            StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("summary", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("alt", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("alttext", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("city", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("countryname",
            StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("displayname",
            StringComparison.Ordinal);
   }

   private static bool IsLikelyMachineValue(string value)
   {
      return value.Contains("://", StringComparison.Ordinal) ||
         value.Contains("/", StringComparison.Ordinal) ||
         value.Contains("rrn:", StringComparison.Ordinal) ||
         value.Contains("urn:", StringComparison.Ordinal) ||
         value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) ||
         value.All(char.IsDigit) ||
         Regex.IsMatch(
            value,
            @"^[0-9a-fA-F]{12,}$",
            RegexOptions.CultureInvariant
         );
   }

   private static bool IsLikelyHumanReadable(string value)
   {
      if(!value.Any(char.IsLetter))
      {
         return false;
      }

      if(value.Contains(" ", StringComparison.Ordinal))
      {
         return true;
      }

      return value.Length <= 5 && value.All(char.IsUpper);
   }

   private static bool IsLikelyReadableStructuredPhrase(string value)
   {
      if(!IsLikelyHumanReadable(value))
      {
         return false;
      }

      var tokens = value.Split(
         ' ',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      );

      if(tokens.Length < 2 || tokens.Length > 4)
      {
         return false;
      }

      if(tokens.Any(IsCommonStructuredLabelToken))
      {
         return false;
      }

      return tokens.All(token =>
         token.All(character =>
            char.IsLetter(character) ||
            character is '-' or '\'' or '’'
         )
      );
   }

   private static bool IsCommonStructuredLabelToken(string token)
   {
      var normalizedToken = token.Trim().ToLowerInvariant();

      return normalizedToken is
         "count" or
         "no." or
         "no" or
         "name" or
         "title" or
         "label" or
         "text" or
         "description" or
         "summary" or
         "status" or
         "type" or
         "category" or
         "class" or
         "group" or
         "rank" or
         "round" or
         "date" or
         "time" or
         "priority" or
         "eligible" or
         "entry" or
         "entries" or
         "item" or
         "items" or
         "value" or
         "values" or
         "country" or
         "city" or
         "table" or
         "row" or
         "column" or
         "cell" or
         "id" or
         "code" or
         "page" or
         "section" or
         "link" or
         "url";
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

   private sealed class WebPageFetchException : Exception
   {
      public WebPageFetchException(
         WebPageFetchErrorKind errorKind,
         string message
      )
         : base(message)
      {
         ErrorKind = errorKind;
      }

      public WebPageFetchErrorKind ErrorKind { get; }
   }

   private async Task<WebPageContent?> FetchCurlPageAsync(
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var curlPath = "curl";
      var output = await RunCurlAsync(curlPath, absoluteUrl, cancellationToken);

      if(string.IsNullOrWhiteSpace(output))
      {
         return null;
      }

      return ParseCurlOutput(output, absoluteUrl);
   }

   private static async Task<string> RunCurlAsync(
      string curlPath,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var processStartInfo = new ProcessStartInfo
      {
         FileName = curlPath,
         RedirectStandardOutput = true,
         RedirectStandardError = true,
         UseShellExecute = false,
         CreateNoWindow = true
      };

      processStartInfo.ArgumentList.Add("--silent");
      processStartInfo.ArgumentList.Add("--show-error");
      processStartInfo.ArgumentList.Add("--location");
      processStartInfo.ArgumentList.Add("--compressed");
      processStartInfo.ArgumentList.Add("--max-time");
      processStartInfo.ArgumentList.Add("30");
      processStartInfo.ArgumentList.Add("--output");
      processStartInfo.ArgumentList.Add("-");
      processStartInfo.ArgumentList.Add("--write-out");
      processStartInfo.ArgumentList.Add(
         "\n__SESPORT_CURL_STATUS__:%{http_code}\n"
      );
      processStartInfo.ArgumentList.Add(absoluteUrl.ToString());

      using var process = Process.Start(processStartInfo);

      if(process is null)
      {
         return string.Empty;
      }

      using var cancellationRegistration = cancellationToken.Register(
         () =>
         {
            try
            {
               if(!process.HasExited)
               {
                  process.Kill(entireProcessTree: true);
               }
            }
            catch
            {
            }
         }
      );

      var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
      var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

      await process.WaitForExitAsync(cancellationToken);

      var stdout = await stdoutTask;
      _ = await stderrTask;

      return stdout;
   }

   private static WebPageContent? ParseCurlOutput(
      string output,
      Uri absoluteUrl
   )
   {
      var marker = "\n__SESPORT_CURL_STATUS__:";
      var markerIndex = output.LastIndexOf(
         marker,
         StringComparison.Ordinal
      );

      if(markerIndex < 0)
      {
         return BuildFailureContent(
            absoluteUrl,
            null,
            WebPageFetchErrorKind.BrowserBlocked,
            "Curl fallback returned an unexpected response."
         );
      }

      var body = output[..markerIndex];
      var statusLine = output[(markerIndex + marker.Length)..].Trim();
      var statusCode = statusLine.Split(
         '\n',
         StringSplitOptions.RemoveEmptyEntries
      )[0].Trim();

      if(!string.Equals(statusCode, "200", StringComparison.Ordinal))
      {
         return BuildFailureContent(
            absoluteUrl,
            null,
            WebPageFetchErrorKind.BrowserBlocked,
            $"Curl fallback returned HTTP {statusCode}."
         );
      }

      var title = ExtractHtmlTitle(body);
      var text = ExtractHtmlTextWithEmbeddedState(body);

      if(string.IsNullOrWhiteSpace(text))
      {
         return BuildFailureContent(
            absoluteUrl,
            title,
            WebPageFetchErrorKind.BrowserBlocked,
            "Curl fallback produced no text."
         );
      }

      if(IsBlockedPage(title, text))
      {
         return BuildFailureContent(
            absoluteUrl,
            title,
            WebPageFetchErrorKind.BrowserBlocked,
            "Curl fallback was blocked."
         );
      }

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
