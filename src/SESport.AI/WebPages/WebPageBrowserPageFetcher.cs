using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace SESport.AI.WebPages;

internal static class WebPageBrowserPageFetcher
{
   private static readonly object StrategyHistoryLock = new();
   private static readonly Dictionary<string, BrowserStrategyHistory>
      StrategyHistoryByUrl = new(StringComparer.Ordinal);

   private static readonly BrowserStrategy[] BrowserStrategies =
   [
      new("chromium-bundled", BrowserEngine.Chromium, null, true),
      new("chromium-channel", BrowserEngine.Chromium, "chromium", false),
      new("chrome-channel", BrowserEngine.Chromium, "chrome", false),
      new("firefox-bundled", BrowserEngine.Firefox, null, false),
      new("webkit-bundled", BrowserEngine.Webkit, null, false)
   ];

   internal static async Task<WebPageContent?> FetchAsync(
      ILogger logger,
      Func<Task<string>> browserUserAgentFetcher,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var fetchId = Guid.NewGuid().ToString("N")[..8];
      var fetchStopwatch = Stopwatch.StartNew();
      logger.LogInformation(
         "Playwright fetch {FetchId} started for {Url}.",
         fetchId,
         absoluteUrl
      );

      try
      {
         if(!TryReserveNextStrategy(absoluteUrl, out var strategy))
         {
            logger.LogWarning(
               "No unused Playwright strategy remains for {Url}; " +
               "skipping browser fetch.",
               absoluteUrl
            );
            return null;
         }

         var browserUserAgent = await browserUserAgentFetcher()
            .WaitAsync(cancellationToken);
         logger.LogInformation(
            "Playwright fetch {FetchId} obtained browser user agent for " +
            "{Url}.",
            fetchId,
            absoluteUrl
         );

         logger.LogInformation(
            "Playwright fetch {FetchId} creating browser session for " +
            "{Url}.",
            fetchId,
            absoluteUrl
         );
         using var playwright = await Playwright.CreateAsync()
            .WaitAsync(cancellationToken);
         logger.LogInformation(
            "Playwright fetch {FetchId} created browser session for {Url}.",
            fetchId,
            absoluteUrl
         );
         WebPageContent? lastBlockedContent = null;
         WebPageFetchException? lastException = null;

         while(true)
         {
            var strategyStopwatch = Stopwatch.StartNew();
            logger.LogInformation(
               "Playwright strategy {Strategy} ({FetchId}) started for " +
               "{Url}.",
               strategy.Id,
               fetchId,
               absoluteUrl
            );

            try
            {
               var content = await FetchWithStrategyAsync(
                  logger,
                  fetchId,
                  playwright,
                  strategy,
                  browserUserAgent,
                  absoluteUrl,
                  cancellationToken
               );

               if(IsUsableContent(content))
               {
                  logger.LogInformation(
                     "Playwright strategy {Strategy} ({FetchId}) succeeded " +
                     "for {Url} after {ElapsedMilliseconds} ms.",
                     strategy.Id,
                     fetchId,
                     absoluteUrl,
                     strategyStopwatch.ElapsedMilliseconds
                  );
                  return content;
               }

               logger.LogWarning(
                  "Playwright strategy {Strategy} ({FetchId}) returned " +
                  "unusable content for {Url} after " +
                  "{ElapsedMilliseconds} ms. ErrorKind: {ErrorKind}.",
                  strategy.Id,
                  fetchId,
                  absoluteUrl,
                  strategyStopwatch.ElapsedMilliseconds,
                  content?.FetchErrorKind
               );

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
                  WebPageFetchLogging.SummarizeException(exception),
                  exception,
                  strategy.Id
               );
               logger.LogWarning(
                  "Playwright strategy {Strategy} ({FetchId}) timed out " +
                  "for {Url} after {ElapsedMilliseconds} ms. Reason: " +
                  "{Reason}.",
                  strategy.Id,
                  fetchId,
                  absoluteUrl,
                  strategyStopwatch.ElapsedMilliseconds,
                  WebPageFetchLogging.SummarizeException(exception)
               );
            }
            catch(PlaywrightException exception)
            {
               lastException = new WebPageFetchException(
                  WebPageFetchErrorKind.BrowserBlocked,
                  WebPageFetchLogging.SummarizeException(exception),
                  exception,
                  strategy.Id
               );
               logger.LogWarning(
                  "Playwright strategy {Strategy} ({FetchId}) failed for " +
                  "{Url} after {ElapsedMilliseconds} ms. Reason: {Reason}.",
                  strategy.Id,
                  fetchId,
                  absoluteUrl,
                  strategyStopwatch.ElapsedMilliseconds,
                  WebPageFetchLogging.SummarizeException(exception)
               );
            }

            if(!TryReserveNextStrategy(absoluteUrl, out strategy))
            {
               break;
            }
         }

         if(lastBlockedContent is not null)
         {
            logger.LogWarning(
               "Playwright fetch {FetchId} completed without usable " +
               "content for {Url} after {ElapsedMilliseconds} ms; " +
               "returning the last browser result.",
               fetchId,
               absoluteUrl,
               fetchStopwatch.ElapsedMilliseconds
            );
            return lastBlockedContent;
         }

         if(lastException is not null)
         {
            throw lastException;
         }

         logger.LogWarning(
            "Playwright fetch {FetchId} returned no content for {Url} " +
            "after {ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            fetchStopwatch.ElapsedMilliseconds
         );
         return null;
      }
      catch(OperationCanceledException)
      {
         logger.LogWarning(
            "Playwright fetch {FetchId} canceled for {Url} after " +
            "{ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            fetchStopwatch.ElapsedMilliseconds
         );
         throw;
      }
      catch(TimeoutException exception)
      {
         logger.LogWarning(
            "Playwright fetch {FetchId} timed out for {Url} after " +
            "{ElapsedMilliseconds} ms. Reason: {Reason}.",
            fetchId,
            absoluteUrl,
            fetchStopwatch.ElapsedMilliseconds,
            WebPageFetchLogging.SummarizeException(exception)
         );
         throw new WebPageFetchException(
            WebPageFetchErrorKind.Timeout,
            WebPageFetchLogging.SummarizeException(exception),
            exception
         );
      }
      catch(PlaywrightException exception)
      {
         logger.LogWarning(
            "Playwright fetch {FetchId} failed for {Url} after " +
            "{ElapsedMilliseconds} ms. Reason: {Reason}.",
            fetchId,
            absoluteUrl,
            fetchStopwatch.ElapsedMilliseconds,
            WebPageFetchLogging.SummarizeException(exception)
         );
         throw new WebPageFetchException(
            WebPageFetchErrorKind.BrowserBlocked,
            WebPageFetchLogging.SummarizeException(exception),
            exception
         );
      }
      finally
      {
         logger.LogInformation(
            "Playwright fetch {FetchId} finished for {Url} after " +
            "{ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            fetchStopwatch.ElapsedMilliseconds
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
      ILogger logger,
      string fetchId,
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

      var browserStopwatch = Stopwatch.StartNew();
      logger.LogInformation(
         "Playwright strategy {Strategy} ({FetchId}) launching {Engine} " +
         "for {Url}.",
         strategy.Id,
         fetchId,
         strategy.Engine,
         absoluteUrl
      );
      await using var browser = await browserType.LaunchAsync(
         new BrowserTypeLaunchOptions
         {
            Channel = strategy.Channel,
            Headless = true
         }
      ).WaitAsync(cancellationToken);
      logger.LogInformation(
         "Playwright strategy {Strategy} ({FetchId}) launched browser for " +
         "{Url} after {ElapsedMilliseconds} ms.",
         strategy.Id,
         fetchId,
         absoluteUrl,
         browserStopwatch.ElapsedMilliseconds
      );

      var userAgent = strategy.UseBrowserUserAgent
         ? browserUserAgent
         : null;
      await using var context = await browser.NewContextAsync(
         BuildContextOptions(userAgent)
      ).WaitAsync(cancellationToken);
      logger.LogInformation(
         "Playwright strategy {Strategy} ({FetchId}) created browser " +
         "context for {Url} after {ElapsedMilliseconds} ms.",
         strategy.Id,
         fetchId,
         absoluteUrl,
         browserStopwatch.ElapsedMilliseconds
      );

      await using var page = await context.NewPageAsync()
         .WaitAsync(cancellationToken);
      logger.LogInformation(
         "Playwright strategy {Strategy} ({FetchId}) created page for {Url} " +
         "after {ElapsedMilliseconds} ms.",
         strategy.Id,
         fetchId,
         absoluteUrl,
         browserStopwatch.ElapsedMilliseconds
      );
      var strategyStopwatch = Stopwatch.StartNew();
      logger.LogInformation(
         "Playwright strategy {Strategy} ({FetchId}) started for {Url}.",
         strategy.Id,
         fetchId,
         absoluteUrl
      );

      try
      {
         var navigationResponse = await NavigateAsync(
            logger,
            fetchId,
            strategy.Id,
            page,
            absoluteUrl,
            cancellationToken
         );
         var content = await ReadPageContentAsync(
            logger,
            fetchId,
            strategy.Id,
            page,
            absoluteUrl,
            navigationResponse?.Status,
            cancellationToken
         );

         if(IsUsableContent(content))
         {
            logger.LogInformation(
               "Playwright strategy {Strategy} ({FetchId}) succeeded for " +
               "{Url} after {ElapsedMilliseconds} ms.",
               strategy.Id,
               fetchId,
               absoluteUrl,
               strategyStopwatch.ElapsedMilliseconds
            );
            return content;
         }

         logger.LogWarning(
            "Playwright strategy {Strategy} ({FetchId}) returned unusable " +
            "content for {Url} after " +
            "{ElapsedMilliseconds} ms. ErrorKind: {ErrorKind}.",
            strategy.Id,
            fetchId,
            absoluteUrl,
            strategyStopwatch.ElapsedMilliseconds,
            content?.FetchErrorKind
         );
         return content;
      }
      catch(OperationCanceledException)
      {
         logger.LogWarning(
            "Playwright strategy {Strategy} ({FetchId}) canceled for " +
            "{Url} after " +
            "{ElapsedMilliseconds} ms.",
            strategy.Id,
            fetchId,
            absoluteUrl,
            strategyStopwatch.ElapsedMilliseconds
         );
         throw;
      }
   }

   private static bool TryReserveNextStrategy(
      Uri absoluteUrl,
      out BrowserStrategy strategy
   )
   {
      strategy = null!;
      var now = DateTimeOffset.UtcNow;
      var urlKey = absoluteUrl.AbsoluteUri;

      lock(StrategyHistoryLock)
      {
         RemoveExpiredStrategyHistories(now);

         if(!StrategyHistoryByUrl.TryGetValue(
            urlKey,
            out var history
         ))
         {
            TrimStrategyHistoriesIfNeeded();
            history = new BrowserStrategyHistory();
            StrategyHistoryByUrl.Add(urlKey, history);
         }

         foreach(var candidate in BrowserStrategies)
         {
            if(history.AttemptedStrategyIds.Add(candidate.Id))
            {
               history.LastTouched = now;
               strategy = candidate;
               return true;
            }
         }
      }

      return false;
   }

   private static void RemoveExpiredStrategyHistories(
      DateTimeOffset now
   )
   {
      var expiredKeys = StrategyHistoryByUrl
         .Where(entry => now - entry.Value.LastTouched >=
            WebPageFetchDefaults.BrowserStrategyAttemptMemoryDuration)
         .Select(entry => entry.Key)
         .ToArray();

      foreach(var key in expiredKeys)
      {
         StrategyHistoryByUrl.Remove(key);
      }
   }

   private static void TrimStrategyHistoriesIfNeeded()
   {
      while(StrategyHistoryByUrl.Count >=
         WebPageFetchDefaults.BrowserStrategyAttemptMemoryMaximumUrlCount)
      {
         var oldest = StrategyHistoryByUrl
            .OrderBy(entry => entry.Value.LastTouched)
            .FirstOrDefault();

         if(oldest.Key is null)
         {
            return;
         }

         StrategyHistoryByUrl.Remove(oldest.Key);
      }
   }

   private static async Task<IResponse?> NavigateAsync(
      ILogger logger,
      string fetchId,
      string strategy,
      IPage page,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var stopwatch = Stopwatch.StartNew();
      logger.LogInformation(
         "Playwright navigation ({FetchId}) started for {Url}; " +
         "strategy {Strategy}.",
         fetchId,
         absoluteUrl,
         strategy
      );

      try
      {
         var response = await page.GotoAsync(
            absoluteUrl.ToString(),
            new PageGotoOptions
            {
               WaitUntil = WaitUntilState.DOMContentLoaded,
               Timeout = (float)
                  WebPageFetchDefaults.BrowserNavigationTimeout
                     .TotalMilliseconds
            }
         ).WaitAsync(cancellationToken);

         logger.LogInformation(
            "Playwright navigation ({FetchId}) completed for {Url}; " +
            "strategy {Strategy}, status {Status}, " +
            "after {ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            strategy,
            response?.Status,
            stopwatch.ElapsedMilliseconds
         );
         return response;
      }
      catch(OperationCanceledException)
      {
         logger.LogWarning(
            "Playwright navigation ({FetchId}) canceled for {Url}; " +
            "strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds
         );
         throw;
      }
      catch(TimeoutException)
      {
         // Some SPA pages keep loading long enough to miss the initial
         // DOMContentLoaded wait, but still render useful content after
         // the browser is allowed to continue.
         logger.LogWarning(
            "Playwright navigation ({FetchId}) timed out for {Url}; " +
            "strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms. Continuing with the current page.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds
         );
         return null;
      }
   }

   private static async Task<WebPageContent?> ReadPageContentAsync(
      ILogger logger,
      string fetchId,
      string strategy,
      IPage page,
      Uri absoluteUrl,
      int? navigationStatus,
      CancellationToken cancellationToken
   )
   {
      var readStopwatch = Stopwatch.StartNew();
      logger.LogInformation(
         "Playwright content read ({FetchId}) started for {Url}; " +
         "strategy {Strategy}.",
         fetchId,
         absoluteUrl,
         strategy
      );

      var loadStateStopwatch = Stopwatch.StartNew();
      logger.LogInformation(
         "Playwright NetworkIdle wait ({FetchId}) started for {Url}; " +
         "strategy {Strategy}.",
         fetchId,
         absoluteUrl,
         strategy
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
         ).WaitAsync(cancellationToken);
         logger.LogInformation(
            "Playwright NetworkIdle wait ({FetchId}) completed for {Url}; " +
            "strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            strategy,
            loadStateStopwatch.ElapsedMilliseconds
         );
      }
      catch(OperationCanceledException)
      {
         logger.LogWarning(
            "Playwright NetworkIdle wait ({FetchId}) canceled for {Url}; " +
            "strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            strategy,
            loadStateStopwatch.ElapsedMilliseconds
         );
         throw;
      }
      catch(TimeoutException)
      {
         logger.LogWarning(
            "Playwright NetworkIdle wait ({FetchId}) timed out for {Url}; " +
            "strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms. Continuing.",
            fetchId,
            absoluteUrl,
            strategy,
            loadStateStopwatch.ElapsedMilliseconds
         );
      }
      catch(PlaywrightException exception)
      {
         logger.LogWarning(
            "Playwright NetworkIdle wait ({FetchId}) failed for {Url}; " +
            "strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms. Continuing. Reason: {Reason}.",
            fetchId,
            absoluteUrl,
            strategy,
            loadStateStopwatch.ElapsedMilliseconds,
            WebPageFetchLogging.SummarizeException(exception)
         );
      }

      cancellationToken.ThrowIfCancellationRequested();
      await ScrollThroughPageAsync(
         logger,
         fetchId,
         strategy,
         page,
         absoluteUrl,
         cancellationToken
      );
      await WaitForContentStabilityAsync(
         logger,
         fetchId,
         strategy,
         page,
         absoluteUrl,
         cancellationToken
      );

      logger.LogInformation(
         "Playwright content extraction ({FetchId}) started for {Url}; " +
         "strategy {Strategy}.",
         fetchId,
         absoluteUrl,
         strategy
      );

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
      var headings = WebPageContentFetchSupport.ExtractHtmlHeadings(
         bodyHtml
      );
      var extractedText =
         WebPageContentFetchSupport
            .ExtractHtmlTextWithEmbeddedState(bodyHtml);
      var normalizedText = WebPageContentFetchSupport
         .RemoveTemplateArtifacts(extractedText);
      var visibleText = WebPageContentFetchSupport.ExtractHtmlText(bodyHtml);
      var renderWarning = WebPageContentFetchSupport
         .DetectIncompleteContentWarning(visibleText);

      var blockedSignature = WebPageBlockDetection
         .FindBlockedSignature(
            title,
            visibleText,
            WebPageBlockSource.Browser
         );
      var blockedStatus = navigationStatus is 401 or 403 or 429 &&
         string.IsNullOrWhiteSpace(normalizedText);
      var unsuccessfulStatus = navigationStatus is int httpStatus &&
         (httpStatus < 200 || httpStatus >= 300);
      var softErrorSignature = WebPageBlockDetection
         .FindSoftErrorSignature(title, visibleText);

      if(blockedSignature is not null ||
         blockedStatus ||
         unsuccessfulStatus ||
         softErrorSignature is not null)
      {
         var statusText = navigationStatus is int status
            ? $" HTTP {status}."
            : string.Empty;
         var reason = unsuccessfulStatus
            ? "Browser renderer returned an unsuccessful HTTP response." +
               statusText
            : softErrorSignature is not null
            ? "Browser renderer returned a not-found page: " +
               softErrorSignature + "." + statusText
            : blockedSignature is not null
            ? "Browser renderer returned a blocked page: " +
               blockedSignature + "." + statusText
            : "Browser renderer returned no content." + statusText;
         var errorKind = unsuccessfulStatus || softErrorSignature is not null
            ? WebPageFetchErrorKind.HttpError
            : WebPageFetchErrorKind.BrowserBlocked;

         logger.LogWarning(
            "Playwright content read ({FetchId}) produced a failure for " +
            "{Url}; strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms. ErrorKind: {ErrorKind}.",
            fetchId,
            absoluteUrl,
            strategy,
            readStopwatch.ElapsedMilliseconds,
            errorKind
         );
         return WebPageContentFetchSupport.BuildFailureContent(
            absoluteUrl,
            title,
            errorKind,
            reason,
            "playwright",
            strategy
         );
      }

      var content = new WebPageContent(
         string.IsNullOrWhiteSpace(title) ? absoluteUrlString : title,
         absoluteUrlString,
         WebPageContentFetchSupport.ExtractPublishedAt(renderedHtml),
         headings,
         WebPageContentFetchSupport.ApplyResponseCutoff(
            normalizedText
         ),
         !string.IsNullOrWhiteSpace(normalizedText),
         normalizedText,
         Fetcher: "playwright",
         BrowserStrategy: strategy,
         RenderWarning: renderWarning,
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

      logger.LogInformation(
         "Playwright content read ({FetchId}) completed for {Url}; " +
         "strategy {Strategy}, after " +
         "{ElapsedMilliseconds} ms. Text characters: {TextCharacters}; " +
         "images: {ImageCount}; render warning: {RenderWarning}.",
         fetchId,
         absoluteUrl,
         strategy,
         readStopwatch.ElapsedMilliseconds,
         normalizedText.Length,
         relevantImages.Length,
         renderWarning ?? "none"
      );
      return content;
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

   private sealed class BrowserStrategyHistory
   {
      internal HashSet<string> AttemptedStrategyIds { get; } = [];

      internal DateTimeOffset LastTouched { get; set; }
   }

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
      ILogger logger,
      string fetchId,
      string strategy,
      IPage page,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var stopwatch = Stopwatch.StartNew();
      var steps = 0;
      logger.LogInformation(
         "Playwright page scroll ({FetchId}) started for {Url}; " +
         "strategy {Strategy}.",
         fetchId,
         absoluteUrl,
         strategy
      );

      try
      {
         var timeoutAt = DateTimeOffset.UtcNow.Add(
            WebPageFetchDefaults.BrowserScrollTimeout
         );
         var previousHeight = await GetDocumentHeightAsync(page)
            .WaitAsync(cancellationToken);
         var stableSampleCount = 0;

         for(var step = 0;
            step < WebPageFetchDefaults.BrowserScrollMaxSteps &&
            DateTimeOffset.UtcNow < timeoutAt;
            step++)
         {
            cancellationToken.ThrowIfCancellationRequested();
            await page.EvaluateAsync(
               "window.scrollBy(0, window.innerHeight * 0.75)"
            ).WaitAsync(cancellationToken);
            await Task.Delay(
               WebPageFetchDefaults.BrowserScrollInterval,
               cancellationToken
            );
            steps = step + 1;

            var currentHeight = await GetDocumentHeightAsync(page)
               .WaitAsync(cancellationToken);
            var isAtBottom = await page.EvaluateAsync<bool>(
               "window.scrollY + window.innerHeight >= " +
               "document.documentElement.scrollHeight - 1"
            ).WaitAsync(cancellationToken);
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

         await page.EvaluateAsync("window.scrollTo(0, 0)")
            .WaitAsync(cancellationToken);
         logger.LogInformation(
            "Playwright page scroll ({FetchId}) completed for {Url}; " +
            "strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms with {Steps} steps.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds,
            steps
         );
      }
      catch(OperationCanceledException)
      {
         logger.LogWarning(
            "Playwright page scroll ({FetchId}) canceled for {Url}; " +
            "strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms at step {Steps}.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds,
            steps
         );
         throw;
      }
   }

   private static Task<double> GetDocumentHeightAsync(IPage page)
   {
      return page.EvaluateAsync<double>(
         "document.documentElement.scrollHeight"
      );
   }

   private static async Task WaitForContentStabilityAsync(
      ILogger logger,
      string fetchId,
      string strategy,
      IPage page,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var stopwatch = Stopwatch.StartNew();
      logger.LogInformation(
         "Playwright content stability ({FetchId}) started for {Url}; " +
         "strategy {Strategy}.",
         fetchId,
         absoluteUrl,
         strategy
      );

      try
      {
         var timeoutAt = DateTimeOffset.UtcNow.Add(
            WebPageFetchDefaults.BrowserContentStabilityTimeout
         );
         string? previousText = null;
         var stableSampleCount = 0;
         var stable = false;

         while(DateTimeOffset.UtcNow < timeoutAt)
         {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(
               WebPageFetchDefaults.BrowserContentStabilityInterval,
               cancellationToken
            );

            var currentText = await page.Locator("body").InnerTextAsync()
               .WaitAsync(cancellationToken);
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
                  stable = true;
                  break;
               }
            }
            else
            {
               previousText = currentText;
               stableSampleCount = 0;
            }
         }

         logger.LogInformation(
            "Playwright content stability ({FetchId}) completed for " +
            "{Url}; strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms. Stable: {Stable}.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds,
            stable
         );
      }
      catch(OperationCanceledException)
      {
         logger.LogWarning(
            "Playwright content stability ({FetchId}) canceled for {Url}; " +
            "strategy {Strategy}, after " +
            "{ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds
         );
         throw;
      }
   }
}
