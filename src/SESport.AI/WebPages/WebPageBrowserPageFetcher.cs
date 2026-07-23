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
         await ScrollThroughPageAsync(page, cancellationToken);
         await WaitForContentStabilityAsync(page, cancellationToken);

         var title = await page.TitleAsync();
         var renderedHtml = await page.ContentAsync();
         var renderedRelevantLinks =
            WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
               renderedHtml,
               absoluteUrl
            );
         var relevantImages = await ExtractRelevantImagesAsync(page);
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
               ),
            RelevantImages: relevantImages
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

   private static Task<WebPageImageCandidate[]> ExtractRelevantImagesAsync(
      IPage page
   )
   {
      var script = $$"""
         () => Array.from(document.images)
            .map(image => {
               const width = image.naturalWidth;
               const height = image.naturalHeight;
               const url = image.currentSrc || image.src;
               const details = [
                  url,
                  image.alt,
                  image.className
               ].join(" ").toLowerCase();
               const semanticTerms = [
                  "entry",
                  "start",
                  "result",
                  "driver",
                  "participant",
                  "document",
                  "list"
               ];
               const semanticMatch = semanticTerms.some(
                  term => details.includes(term)
               );
               const inContent = Boolean(image.closest(
                  "main, article, [role='main'], .content, .field"
               ));

               return {
                  url,
                  width,
                  height,
                  alt: image.alt || null,
                  semanticMatch,
                  score:
                     (semanticMatch ? 2 : 0) +
                     (inContent ? 1 : 0) +
                     Math.min(width * height / 1000000, 1)
               };
            })
            .filter(image =>
               (
                  image.semanticMatch ||
                  (
                     image.width >=
                        {{WebPageFetchDefaults.ImageOcrMinimumWidth}} &&
                     image.height >=
                        {{WebPageFetchDefaults.ImageOcrMinimumHeight}} &&
                     image.width * image.height >=
                        {{WebPageFetchDefaults.ImageOcrMinimumArea}}
                  )
               ) &&
               /^https?:/i.test(image.url)
            )
            .filter((image, index, images) =>
               images.findIndex(item => item.url === image.url) === index
            )
            .sort((left, right) => right.score - left.score)
            .slice(
               0,
               {{WebPageFetchDefaults.ImageOcrMaximumCandidateCount}}
            )
            .map(({ url, width, height, alt }) => ({
               url,
               width,
               height,
               alt
            }))
         """;

      return page.EvaluateAsync<WebPageImageCandidate[]>(
         script
      );
   }

   private static async Task ScrollThroughPageAsync(
      IPage page,
      CancellationToken cancellationToken
   )
   {
      var timeoutAt = DateTimeOffset.UtcNow.Add(
         WebPageFetchDefaults.BrowserScrollTimeout
      );
      var previousHeight = await GetDocumentHeightAsync(page);
      var stableSampleCount = 0;

      for(var step = 0;
         step < WebPageFetchDefaults.BrowserScrollMaxSteps &&
         DateTimeOffset.UtcNow < timeoutAt;
         step++)
      {
         cancellationToken.ThrowIfCancellationRequested();
         await page.EvaluateAsync(
            "window.scrollBy(0, window.innerHeight * 0.75)"
         );
         await Task.Delay(
            WebPageFetchDefaults.BrowserScrollInterval,
            cancellationToken
         );

         var currentHeight = await GetDocumentHeightAsync(page);
         var isAtBottom = await page.EvaluateAsync<bool>(
            "window.scrollY + window.innerHeight >= " +
            "document.documentElement.scrollHeight - 1"
         );
         if(isAtBottom && currentHeight == previousHeight)
         {
            stableSampleCount++;
            if(stableSampleCount >=
               WebPageFetchDefaults.BrowserStableScrollSampleCount)
            {
               break;
            }
         }
         else
         {
            stableSampleCount = 0;
         }

         previousHeight = currentHeight;
      }

      await page.EvaluateAsync("window.scrollTo(0, 0)");
   }

   private static Task<double> GetDocumentHeightAsync(IPage page)
   {
      return page.EvaluateAsync<double>(
         "document.documentElement.scrollHeight"
      );
   }

   private static async Task WaitForContentStabilityAsync(
      IPage page,
      CancellationToken cancellationToken
   )
   {
      var timeoutAt = DateTimeOffset.UtcNow.Add(
         WebPageFetchDefaults.BrowserContentStabilityTimeout
      );
      string? previousText = null;
      var stableSampleCount = 0;

      while(DateTimeOffset.UtcNow < timeoutAt)
      {
         cancellationToken.ThrowIfCancellationRequested();
         await Task.Delay(
            WebPageFetchDefaults.BrowserContentStabilityInterval,
            cancellationToken
         );

         var currentText = await page.Locator("body").InnerTextAsync();
         if(string.Equals(
            currentText,
            previousText,
            StringComparison.Ordinal
         ))
         {
            stableSampleCount++;
            if(stableSampleCount >=
               WebPageFetchDefaults.BrowserStableContentSampleCount)
            {
               return;
            }
         }
         else
         {
            previousText = currentText;
            stableSampleCount = 0;
         }
      }
   }
}
