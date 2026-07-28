using Microsoft.Playwright;

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
         var browserUserAgent = await browserUserAgentFetcher()
            .WaitAsync(cancellationToken);
         var browserLikeHeaders =
            WebPageContentFetchSupport.BuildBrowserLikeHeaders(
               browserUserAgent
            );
         using var playwright = await Playwright.CreateAsync()
            .WaitAsync(cancellationToken);
         await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
               Headless = true
            }
         ).WaitAsync(cancellationToken);

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
         ).WaitAsync(cancellationToken);

         await context.AddInitScriptAsync(
            WebPageFetchDefaults.BrowserFingerprintScript
         ).WaitAsync(cancellationToken);

         await using var page = await context.NewPageAsync()
            .WaitAsync(cancellationToken);
         await page.GotoAsync(
            absoluteUrl.ToString(),
            new PageGotoOptions
            {
               WaitUntil = WaitUntilState.DOMContentLoaded,
               Timeout = (float)
                  WebPageFetchDefaults.BrowserNavigationTimeout
                     .TotalMilliseconds
            }
         ).WaitAsync(cancellationToken);

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
            ).WaitAsync(cancellationToken);
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

         var title = await page.TitleAsync()
            .WaitAsync(cancellationToken);
         var renderedHtml = await page.ContentAsync()
            .WaitAsync(cancellationToken);
         var renderedRelevantLinks =
            WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
               renderedHtml,
               absoluteUrl
            );
         var relevantImages = await ExtractRelevantImagesAsync(page)
            .WaitAsync(cancellationToken);
         await page.EvaluateAsync(
            WebPageNormalizationScript.Build()
         ).WaitAsync(cancellationToken);
         var bodyHtml = await page.Locator("body").EvaluateAsync<string>(
            "element => element.innerHTML"
         ).WaitAsync(cancellationToken);
         var normalizedText =
            WebPageContentFetchSupport
               .ExtractHtmlTextWithEmbeddedState(bodyHtml);

         return new WebPageContent(
            string.IsNullOrWhiteSpace(title) ? absoluteUrlString : title,
            absoluteUrlString,
            null,
            [],
            WebPageContentFetchSupport.ApplyResponseCutoff(
               normalizedText
            ),
            !string.IsNullOrWhiteSpace(normalizedText),
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
      catch(TimeoutException exception)
      {
         throw new WebPageFetchException(
            WebPageFetchErrorKind.Timeout,
            exception.Message,
            exception
         );
      }
      catch(PlaywrightException exception)
      {
         throw new WebPageFetchException(
            WebPageFetchErrorKind.BrowserBlocked,
            exception.Message,
            exception
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
