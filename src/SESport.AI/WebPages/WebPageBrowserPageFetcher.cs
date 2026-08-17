using Microsoft.Playwright;
using System.Text.Json;

namespace SESport.AI.WebPages;

internal static class WebPageBrowserPageFetcher
{
   private static readonly BrowserStrategy[] BrowserStrategies =
   [
      new("chromium-bundled", BrowserEngine.Chromium, null, true),
      new("chromium-channel", BrowserEngine.Chromium, "chromium", false),
      new("chrome-channel", BrowserEngine.Chromium, "chrome", false),
      new("firefox-bundled", BrowserEngine.Firefox, null, false),
      new("webkit-bundled", BrowserEngine.Webkit, null, false)
   ];

   internal static async Task<WebPageContent?> FetchAsync(
      Func<Task<string>> browserUserAgentFetcher,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var browserUserAgent = await browserUserAgentFetcher()
            .WaitAsync(cancellationToken);
         using var playwright = await Playwright.CreateAsync()
            .WaitAsync(cancellationToken);
         WebPageContent? lastBlockedContent = null;
         WebPageFetchException? lastException = null;

         foreach(var strategy in BrowserStrategies)
         {
            try
            {
               var content = await FetchWithStrategyAsync(
                  playwright,
                  strategy,
                  browserUserAgent,
                  absoluteUrl,
                  cancellationToken
               );

               if(IsUsableContent(content))
               {
                  return content;
               }

               if(content?.FetchErrorKind is not null)
               {
                  lastBlockedContent = content;
               }
            }
            catch(OperationCanceledException)
               when(cancellationToken.IsCancellationRequested)
            {
               throw;
            }
            catch(TimeoutException exception)
            {
               lastException = new WebPageFetchException(
                  WebPageFetchErrorKind.Timeout,
                  exception.Message,
                  exception,
                  strategy.Id
               );
            }
            catch(PlaywrightException exception)
            {
               lastException = new WebPageFetchException(
                  WebPageFetchErrorKind.BrowserBlocked,
                  exception.Message,
                  exception,
                  strategy.Id
               );
            }
         }

         if(lastBlockedContent is not null)
         {
            return lastBlockedContent;
         }

         if(lastException is not null)
         {
            throw lastException;
         }

         return null;
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

   internal static BrowserNewContextOptions BuildContextOptions(
      string? browserUserAgent = null
   )
   {
      var options = new BrowserNewContextOptions
      {
         Locale = WebPageFetchDefaults.BrowserLocale,
         ViewportSize = new ViewportSize
         {
            Width = WebPageFetchDefaults.BrowserViewportWidth,
            Height = WebPageFetchDefaults.BrowserViewportHeight
         }
      };

      if(!string.IsNullOrWhiteSpace(browserUserAgent))
      {
         options.UserAgent = browserUserAgent;
      }

      return options;
   }

   private static async Task<WebPageContent?> FetchWithStrategyAsync(
      IPlaywright playwright,
      BrowserStrategy strategy,
      string browserUserAgent,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var browserType = strategy.Engine switch
      {
         BrowserEngine.Chromium => playwright.Chromium,
         BrowserEngine.Firefox => playwright.Firefox,
         BrowserEngine.Webkit => playwright.Webkit,
         _ => throw new ArgumentOutOfRangeException()
      };

      await using var browser = await browserType.LaunchAsync(
         new BrowserTypeLaunchOptions
         {
            Channel = strategy.Channel,
            Headless = true
         }
      ).WaitAsync(cancellationToken);

      var userAgent = strategy.UseBrowserUserAgent
         ? browserUserAgent
         : null;
      await using var context = await browser.NewContextAsync(
         BuildContextOptions(userAgent)
      ).WaitAsync(cancellationToken);

      await using var page = await context.NewPageAsync()
         .WaitAsync(cancellationToken);
      WebPageContent? lastContent = null;

      for(var attempt = 1;
         attempt <= WebPageFetchDefaults.BrowserNavigationRetryAttempts;
         attempt++)
      {
         var navigationResponse = await NavigateAsync(
            page,
            absoluteUrl,
            cancellationToken
         );
         var content = await ReadPageContentAsync(
            page,
            strategy.Id,
            absoluteUrl,
            navigationResponse?.Status,
            cancellationToken
         );

         if(IsUsableContent(content))
         {
            return content;
         }

         lastContent = content;

         if(attempt < WebPageFetchDefaults.BrowserNavigationRetryAttempts)
         {
            await Task.Delay(
               WebPageFetchDefaults.BrowserNavigationRetryDelay,
               cancellationToken
            );
         }
      }

      return lastContent;
   }

   private static async Task<IResponse?> NavigateAsync(
      IPage page,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      try
      {
         return await page.GotoAsync(
            absoluteUrl.ToString(),
            new PageGotoOptions
            {
               WaitUntil = WaitUntilState.DOMContentLoaded,
               Timeout = (float)
                  WebPageFetchDefaults.BrowserNavigationTimeout
                     .TotalMilliseconds
            }
         ).WaitAsync(cancellationToken);
      }
      catch(TimeoutException)
      {
         // Some SPA pages keep loading long enough to miss the initial
         // DOMContentLoaded wait, but still render useful content after
         // the browser is allowed to continue.
         return null;
      }
   }

   private static async Task<WebPageContent?> ReadPageContentAsync(
      IPage page,
      string browserStrategy,
      Uri absoluteUrl,
      int? navigationStatus,
      CancellationToken cancellationToken
   )
   {
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

      var absoluteUrlString = absoluteUrl.ToString();
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

      var blockedSignature = WebPageBlockDetection
         .FindBlockedSignature(
            title,
            normalizedText,
            WebPageBlockSource.Browser
         );
      var blockedStatus = navigationStatus is 401 or 403 or 429 &&
         string.IsNullOrWhiteSpace(normalizedText);

      if(blockedSignature is not null || blockedStatus)
      {
         var statusText = navigationStatus is int status
            ? $" HTTP {status}."
            : string.Empty;
         var reason = blockedSignature is not null
            ? "Browser renderer returned a blocked page: " +
               blockedSignature + "." + statusText
            : "Browser renderer returned no content." + statusText;

         return WebPageContentFetchSupport.BuildFailureContent(
            absoluteUrl,
            title,
            WebPageFetchErrorKind.BrowserBlocked,
            reason,
            "playwright",
            browserStrategy
         );
      }

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
         BrowserStrategy: browserStrategy,
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

   private static bool IsUsableContent(WebPageContent? content)
   {
      return content is not null &&
         content.FetchErrorKind is null &&
         string.IsNullOrWhiteSpace(content.FetchErrorMessage) &&
         (content.HasBodyText ||
            content.RelevantImages is { Count: > 0 });
   }

   private enum BrowserEngine
   {
      Chromium,
      Firefox,
      Webkit
   }

   private sealed record BrowserStrategy(
      string Id,
      BrowserEngine Engine,
      string? Channel,
      bool UseBrowserUserAgent
   );

   private static async Task<WebPageImageCandidate[]>
      ExtractRelevantImagesAsync(
      IPage page
   )
   {
      var script = $$"""
         () => JSON.stringify(
            Array.from(document.images)
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
         )
         """;

      var imagesJson = await page.EvaluateAsync<string>(
         script
      );
      var images = JsonSerializer.Deserialize<WebPageImageCandidate[]>(
         imagesJson ?? "[]",
         new JsonSerializerOptions(JsonSerializerDefaults.Web)
      );

      return images ?? [];
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
