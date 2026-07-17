using System.Text.Json;

using Microsoft.Playwright;

using SESport.Core.Configuration;

namespace SESport.AI.WebPages;

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
               Locale = WebPageFetchDefaults.BrowserLocale,
               ExtraHTTPHeaders = browserLikeHeaders,
               ViewportSize = new ViewportSize
               {
                  Width = WebPageFetchDefaults.BrowserViewportWidth,
                  Height = WebPageFetchDefaults.BrowserViewportHeight
               }
            }
         );

         await context.AddInitScriptAsync(
            WebPageFetchDefaults.BrowserFingerprintScript
         );

         await using var page = await context.NewPageAsync();
         await page.GotoAsync(
            absoluteUrl.ToString(),
            new PageGotoOptions
            {
               WaitUntil = WaitUntilState.DOMContentLoaded,
               Timeout = (float)
                  WebPageFetchDefaults.BrowserNavigationTimeout
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
                     WebPageFetchDefaults.BrowserLoadStateTimeout
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
         var renderedHtml = await page.ContentAsync();
         var renderedRelevantLinks =
            WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
               renderedHtml,
               absoluteUrl
            );
         await page.EvaluateAsync(
            WebPageNormalizationScript.Build(),
            JsonSerializer.Serialize(
               WebPageContentFetchSupport.CountryNamesByCode
            )
         );
         var bodyHtml = await page.Locator("body").EvaluateAsync<string>(
            "element => element.innerHTML"
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
            Fetcher: "playwright",
            RelevantLinks:
               WebPageContentFetchSupport.MergeRelevantLinks(
                  renderedRelevantLinks,
                  WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
                     bodyHtml,
                     absoluteUrl
                  )
               )
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
