using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      browserPageFetcher;

   [ActivatorUtilitiesConstructor]
   public WebPageContentClient(HttpClient httpClient)
      : this(httpClient, null)
   {
   }

   public WebPageContentClient(
      HttpClient httpClient,
      Func<Uri, CancellationToken, Task<WebPageContent?>>? browserPageFetcher
   )
   {
      this.httpClient = httpClient;
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
      catch(TimeoutException)
      {
         return null;
      }
      catch(PlaywrightException)
      {
         return null;
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
         var countryNamesJson = JsonSerializer.Serialize(CountryNamesByCode);
         await page.EvaluateAsync(
            """
            (countryNamesJson) => {
               const countryNames = JSON.parse(countryNamesJson);
               const flagExtensions = '(?:svg|png|gif)';
               const flagClassPattern = /(?:^|\s)flag--([a-z0-9_]+)(?:\s|$)/i;
               const flagCodePattern = /^[a-z]{2,3}$/i;

               function getFlagCode(source) {
                  if(typeof source !== 'string' || source === '') {
                     return null;
                  }

                  const directMatch =
                     source.match(
                        new RegExp(
                           `/svg/flag(?:s)?/([A-Z]{2})\\.` +
                           flagExtensions + '$',
                           'i'
                        )
                     ) ||
                     source.match(
                        new RegExp(
                           `/img/flag-([a-z]{2})\\.` +
                           flagExtensions + '$',
                           'i'
                        )
                     ) ||
                     source.match(
                        new RegExp(
                           `/Flags/([^/?#]+)\\.` +
                           flagExtensions + '$',
                           'i'
                        )
                     ) ||
                     source.match(
                        new RegExp(
                           '/\\/Flag_of_([A-Za-z_]+)\\.svg\\/[^/?#]+' +
                           '\\.(?:svg|png|gif)$',
                           'i'
                        )
                     );

                  if(directMatch) {
                     return directMatch[1];
                  }

                  const nextImageMatch = source.match(
                     /\/_next\/image\?[^?#]*\burl=([^&\s,]+)/i
                  );

                  if(!nextImageMatch) {
                     return null;
                  }

                  let decodedSource = '';

                  try {
                     decodedSource =
                        decodeURIComponent(nextImageMatch[1]);
                  }
                  catch {
                     return null;
                  }

                  const nextImageFlagMatch =
                     decodedSource.match(
                        /\/countries\/([a-z]{2})\.(?:svg|png|gif)$/i
                     ) ||
                     decodedSource.match(
                        /\/flags?\/([a-z]{2})\.(?:svg|png|gif)$/i
                     ) ||
                     decodedSource.match(
                        /\/Flag_of_([A-Za-z_]+)\.svg$/i
                     );

                  return nextImageFlagMatch?.[1] || null;
               }

               function getClassFlagCode(element) {
                  const className = element.getAttribute('class') || '';
                  const dataClass = element.getAttribute('data-class') || '';
                  const classMatch = className.match(flagClassPattern);
                  const dataClassMatch = dataClass.match(flagClassPattern);

                  return classMatch?.[1] || dataClassMatch?.[1] || null;
               }

               function getAltFlagCode(element) {
                  const alt = (element.getAttribute('alt') || '').trim();

                  if(!flagCodePattern.test(alt)) {
                     return null;
                  }

                  return alt;
               }

               document.querySelectorAll(
                  'img, [class*="flag--"], [data-class*="flag--"]'
               ).forEach((element) => {
                  const code =
                     element.tagName.toLowerCase() === 'img'
                        ? getFlagCode(element.getAttribute('src') || '') ||
                           getFlagCode(element.getAttribute('srcset') || '')
                           || getAltFlagCode(element)
                        : getClassFlagCode(element);

                  if(!code) {
                     return;
                  }

                  const labelKey = code.replaceAll('_', ' ');
                  const label = countryNames[labelKey.toUpperCase()] ||
                     element.getAttribute('alt') ||
                     labelKey;

                  if(label) {
                     element.replaceWith(
                        document.createTextNode(` ${label} `)
                     );
                  }
               });

               document.querySelectorAll(
                  'nav, footer, aside, [role="dialog"], ' +
                  '[role="banner"], [aria-modal="true"], ' +
                  '[class*="modal"], [class*="overlay"], ' +
                  '[class*="consent"], [class*="privacy"], ' +
                  '[class*="banner"]'
               ).forEach((element) => element.remove());
            }
            """,
            countryNamesJson
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
