using System.Text.Json;
using Microsoft.Playwright;

namespace SESport.AI.Providers;

internal static class WebPageBrowserPageFetcher
{
   internal static async Task<WebPageContent?> FetchAsync(
      Func<Task<string>> browserUserAgentFetcher,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var absoluteUrlString = absoluteUrl.ToString();
         var browserUserAgent = await browserUserAgentFetcher();
         var browserLikeHeaders =
            WebPageContentFetchSupport.BuildBrowserLikeHeaders(
               browserUserAgent
            );
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
               Locale = "en-US",
               ExtraHTTPHeaders = browserLikeHeaders,
               ViewportSize = new ViewportSize
               {
                  Width = 1440,
                  Height = 2400
               }
            }
         );

         await context.AddInitScriptAsync(
            """
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
            """
         );

         await using var page = await context.NewPageAsync();
         await page.GotoAsync(
            absoluteUrl.ToString(),
            new PageGotoOptions
            {
               WaitUntil = WaitUntilState.DOMContentLoaded,
               Timeout = (float)
                  WebPageContentFetchSupport.BrowserNavigationTimeout
                  .TotalMilliseconds
            }
         );

         try
         {
            await page.WaitForLoadStateAsync(
               LoadState.NetworkIdle,
               new PageWaitForLoadStateOptions
               {
                  Timeout = (float)
                     WebPageContentFetchSupport.BrowserLoadStateTimeout
                     .TotalMilliseconds
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
            JsonSerializer.Serialize(
               WebPageContentFetchSupport.CountryNamesByCode
            )
         );
         var visibleText = await page.Locator("body").InnerTextAsync();
         var normalizedText =
            WebPageContentFetchSupport.NormalizeText(visibleText);

         return new WebPageContent(
            string.IsNullOrWhiteSpace(title) ? absoluteUrlString : title,
            absoluteUrlString,
            null,
            [],
            WebPageContentFetchSupport.ApplyResponseCutoff(
               normalizedText
            ),
            !string.IsNullOrWhiteSpace(visibleText),
            normalizedText,
            Fetcher: "browser"
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
}
