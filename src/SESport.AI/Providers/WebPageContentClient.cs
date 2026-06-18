using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using UglyToad.PdfPig;

using SESport.AI.Interfaces;

namespace SESport.AI.Providers;

public sealed class WebPageContentClient : IWebPageContentClient
{
   private const string BrowserUserAgent =
      "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
      "(KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36";
   private static readonly TimeSpan BrowserNavigationTimeout =
      TimeSpan.FromSeconds(30);
   private static readonly TimeSpan BrowserLoadStateTimeout =
      TimeSpan.FromSeconds(30);
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

      return await browserPageFetcher(absoluteUrl, cancellationToken);
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
         var visibleText = await page.Locator("body").InnerTextAsync();

         return new WebPageContent(
            string.IsNullOrWhiteSpace(title) ? absoluteUrl.ToString() : title,
            absoluteUrl.ToString(),
            null,
            [],
            NormalizeText(visibleText),
            !string.IsNullOrWhiteSpace(visibleText),
            NormalizeText(visibleText)
         );
      }
      catch(OperationCanceledException)
      {
         throw;
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
            text,
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
         .Select(page => page.Text)
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

   private static string NormalizeText(string? text)
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         return string.Empty;
      }

      return text.Replace("\r", "\n", StringComparison.Ordinal).Trim();
   }
}
