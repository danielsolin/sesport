using System.Diagnostics;

using Microsoft.Playwright;

namespace SESport.AI.WebPages;

internal sealed record WebPageBrowserRenderResult(
   string FullHtml,
   string BodyHtml,
   string Title,
   IReadOnlyList<WebPageImageCandidate> RelevantImages,
   int? NavigationStatus,
   string StrategyId
);

internal sealed record WebPageBrowserStrategyAttempt(
   string StrategyId,
   bool Launched,
   bool Rendered,
   int? NavigationStatus,
   string? FailureSummary,
   WebPageFetchErrorKind? ErrorKind
);

internal sealed record WebPageBrowserOutcome(
   WebPageBrowserRenderResult? Render,
   IReadOnlyList<WebPageBrowserStrategyAttempt> Attempts
);

internal static class WebPageBrowserPageFetcher
{
   internal static async Task<WebPageBrowserOutcome> FetchAsync(
      ILogger logger,
      Func<Task<string>> browserUserAgentFetcher,
      Uri absoluteUrl,
      IReadOnlyList<BrowserStrategyDescriptor> strategies,
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

      var attempts = new List<WebPageBrowserStrategyAttempt>();
      WebPageBrowserRenderResult? lastRender = null;

      try
      {
         var browserUserAgent = await browserUserAgentFetcher()
            .WaitAsync(cancellationToken);

         using var playwright = await Playwright.CreateAsync()
            .WaitAsync(cancellationToken);

         foreach(var strategy in strategies)
         {
            cancellationToken.ThrowIfCancellationRequested();

            var (attempt, render) = await TryStrategyAsync(
               logger,
               fetchId,
               playwright,
               strategy,
               browserUserAgent,
               absoluteUrl,
               cancellationToken
            );

            attempts.Add(attempt);

            if(render is not null)
            {
               lastRender = render;

               // A usable render ends the strategy loop. An empty,
               // blocked, or challenged render does not: the remaining
               // strategies may still get through.
               var assessment = WebPageHtmlCandidate.FromRendered(
                  render.FullHtml,
                  render.BodyHtml,
                  render.Title,
                  render.RelevantImages,
                  absoluteUrl
               ).Assess(WebPageBlockSource.Browser);

               var challenged = render.NavigationStatus is
                  401 or 403 or 429 or >= 500;

               if(!challenged &&
                  assessment.Classification is not
                     (WebPageContentClassification.Blocked or
                      WebPageContentClassification.Empty or
                      WebPageContentClassification.NeedsRendering))
               {
                  break;
               }
            }
         }
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
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
      catch(Exception exception)
      {
         logger.LogWarning(
            "Playwright fetch {FetchId} failed for {Url} after " +
            "{ElapsedMilliseconds} ms. Reason: {Reason}.",
            fetchId,
            absoluteUrl,
            fetchStopwatch.ElapsedMilliseconds,
            WebPageFetchLogging.SummarizeException(exception)
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

      return new WebPageBrowserOutcome(lastRender, attempts);
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

   private static async Task<
      (WebPageBrowserStrategyAttempt Attempt,
       WebPageBrowserRenderResult? Render)
   > TryStrategyAsync(
      ILogger logger,
      string fetchId,
      IPlaywright playwright,
      BrowserStrategyDescriptor strategy,
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

      var strategyStopwatch = Stopwatch.StartNew();
      logger.LogInformation(
         "Playwright strategy {Strategy} ({FetchId}) launching " +
         "{Engine} for {Url}.",
         strategy.Id,
         fetchId,
         strategy.Engine,
         absoluteUrl
      );

      var launched = false;
      try
      {
         await using var browser = await browserType.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
               Channel = strategy.Channel,
               Headless = true
            }
         ).WaitAsync(cancellationToken);
         launched = true;

         var userAgent = strategy.UseBrowserUserAgent
            ? browserUserAgent
            : null;
         await using var context = await browser.NewContextAsync(
            BuildContextOptions(userAgent)
         ).WaitAsync(cancellationToken);

         await using var page = await context.NewPageAsync()
            .WaitAsync(cancellationToken);

         var navigationResponse = await NavigateAsync(
            logger,
            fetchId,
            strategy.Id,
            page,
            absoluteUrl,
            cancellationToken
         );
         var navigationStatus = navigationResponse?.Status;

         var render = await ReadRenderAsync(
            logger,
            fetchId,
            strategy.Id,
            page,
            absoluteUrl,
            navigationStatus,
            cancellationToken
         );

         logger.LogInformation(
            "Playwright strategy {Strategy} ({FetchId}) rendered {Url} " +
            "after {ElapsedMilliseconds} ms.",
            strategy.Id,
            fetchId,
            absoluteUrl,
            strategyStopwatch.ElapsedMilliseconds
         );

         return (
            new WebPageBrowserStrategyAttempt(
               strategy.Id,
               true,
               true,
               navigationStatus,
               null,
               null
            ),
            render
         );
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(TimeoutException exception)
      {
         logger.LogWarning(
            "Playwright strategy {Strategy} ({FetchId}) timed out for " +
            "{Url} after {ElapsedMilliseconds} ms. Reason: {Reason}.",
            strategy.Id,
            fetchId,
            absoluteUrl,
            strategyStopwatch.ElapsedMilliseconds,
            WebPageFetchLogging.SummarizeException(exception)
         );

         return (
            new WebPageBrowserStrategyAttempt(
               strategy.Id,
               launched,
               false,
               null,
               WebPageFetchLogging.SummarizeException(exception),
               WebPageFetchErrorKind.Timeout
            ),
            null
         );
      }
      catch(PlaywrightException exception)
      {
         logger.LogWarning(
            "Playwright strategy {Strategy} ({FetchId}) failed for {Url} " +
            "after {ElapsedMilliseconds} ms. Reason: {Reason}.",
            strategy.Id,
            fetchId,
            absoluteUrl,
            strategyStopwatch.ElapsedMilliseconds,
            WebPageFetchLogging.SummarizeException(exception)
         );

         return (
            new WebPageBrowserStrategyAttempt(
               strategy.Id,
               launched,
               false,
               null,
               WebPageFetchLogging.SummarizeException(exception),
               WebPageFetchErrorKind.BrowserBlocked
            ),
            null
         );
      }
      catch(Exception exception)
      {
         logger.LogWarning(
            "Playwright strategy {Strategy} ({FetchId}) failed for {Url} " +
            "after {ElapsedMilliseconds} ms. Reason: {Reason}.",
            strategy.Id,
            fetchId,
            absoluteUrl,
            strategyStopwatch.ElapsedMilliseconds,
            WebPageFetchLogging.SummarizeException(exception)
         );

         return (
            new WebPageBrowserStrategyAttempt(
               strategy.Id,
               launched,
               false,
               null,
               WebPageFetchLogging.SummarizeException(exception),
               WebPageFetchErrorKind.BrowserBlocked
            ),
            null
         );
      }
   }

   private static async Task<WebPageBrowserRenderResult> ReadRenderAsync(
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
            "strategy {Strategy}, after {ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            strategy,
            loadStateStopwatch.ElapsedMilliseconds
         );
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         logger.LogWarning(
            "Playwright NetworkIdle wait ({FetchId}) canceled for {Url}; " +
            "strategy {Strategy}, after {ElapsedMilliseconds} ms.",
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
            "strategy {Strategy}, after {ElapsedMilliseconds} ms. " +
            "Continuing.",
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
            "strategy {Strategy}, after {ElapsedMilliseconds} ms. " +
            "Continuing. Reason: {Reason}.",
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

      var title = await page.TitleAsync()
         .WaitAsync(cancellationToken);
      var renderedHtml = await page.ContentAsync()
         .WaitAsync(cancellationToken);
      var relevantImages = await ExtractRelevantImagesAsync(page)
         .WaitAsync(cancellationToken);
      await page.EvaluateAsync(
         WebPageNormalizationScript.Build()
      ).WaitAsync(cancellationToken);
      var bodyHtml = await page.Locator("body").EvaluateAsync<string>(
         "element => element.innerHTML"
      ).WaitAsync(cancellationToken);

      logger.LogInformation(
         "Playwright content read ({FetchId}) completed for {Url}; " +
         "strategy {Strategy}, after {ElapsedMilliseconds} ms.",
         fetchId,
         absoluteUrl,
         strategy,
         readStopwatch.ElapsedMilliseconds
      );

      return new WebPageBrowserRenderResult(
         renderedHtml,
         bodyHtml,
         title ?? "",
         relevantImages,
         navigationStatus,
         strategy
      );
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
         when(cancellationToken.IsCancellationRequested)
      {
         logger.LogWarning(
            "Playwright navigation ({FetchId}) canceled for {Url}; " +
            "strategy {Strategy}, after {ElapsedMilliseconds} ms.",
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
            "strategy {Strategy}, after {ElapsedMilliseconds} ms. " +
            "Continuing with the current page.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds
         );
         return null;
      }
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
            "strategy {Strategy}, after {ElapsedMilliseconds} ms with " +
            "{Steps} steps.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds,
            steps
         );
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         logger.LogWarning(
            "Playwright page scroll ({FetchId}) canceled for {Url}; " +
            "strategy {Strategy}, after {ElapsedMilliseconds} ms at step " +
            "{Steps}.",
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
            "{Url}; strategy {Strategy}, after {ElapsedMilliseconds} ms. " +
            "Stable: {Stable}.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds,
            stable
         );
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         logger.LogWarning(
            "Playwright content stability ({FetchId}) canceled for {Url}; " +
            "strategy {Strategy}, after {ElapsedMilliseconds} ms.",
            fetchId,
            absoluteUrl,
            strategy,
            stopwatch.ElapsedMilliseconds
         );
         throw;
      }
   }
}
