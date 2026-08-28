using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;

namespace SESport.AI.WebPages;

public sealed class WebPageContentClient : IWebPageContentClient
{
   private const int MaxTransientRetryAttempts =
      WebPageFetchDefaults.MaxTransientRetryAttempts;

   private readonly HttpClient httpClient;
   private readonly ILogger<WebPageContentClient> logger;
   private readonly Func<Task<string>> browserUserAgentFetcher;
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      browserPageFetcher;
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      curlPageFetcher;
   private readonly Func<
      IReadOnlyList<WebPageImageCandidate>,
      CancellationToken,
      Task<string>> imageTextFetcher;

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
         null,
      Func<
         IReadOnlyList<WebPageImageCandidate>,
         CancellationToken,
         Task<string>>? imageTextFetcher = null
   )
   {
      this.httpClient = httpClient;
      this.logger = logger ??
         Microsoft.Extensions.Logging.Abstractions.NullLogger<
            WebPageContentClient>.Instance;
      this.browserUserAgentFetcher = browserUserAgentFetcher ??
         WebPageContentFetchSupport.GetBrowserUserAgentAsync;
      this.browserPageFetcher = browserPageFetcher ??
         ((uri, cancellationToken) =>
            WebPageBrowserPageFetcher.FetchAsync(
               this.browserUserAgentFetcher,
               uri,
               cancellationToken
            ));
      this.curlPageFetcher = curlPageFetcher ??
         ((uri, cancellationToken) =>
            WebPageCurlPageFetcher.FetchAsync(
               this.logger,
               uri,
               cancellationToken
            ));
      this.imageTextFetcher = imageTextFetcher ??
         ((images, cancellationToken) =>
            WebPageImageOcr.ExtractAsync(
               this.httpClient,
               this.logger,
               images,
               cancellationToken
            ));
   }

   public async Task<WebPageContent?> FetchAsync(
      string url,
      CancellationToken cancellationToken
   )
   {
      if(!WebPageUrlPolicy.TryValidate(
         url,
         out var absoluteUrl,
         out _
      ))
      {
         return null;
      }

      return await FetchWithRetryAsync(
         absoluteUrl,
         cancellationToken
      );
   }

   private async Task<WebPageContent?> FetchWithRetryAsync(
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      for(var attempt = 1; attempt <= MaxTransientRetryAttempts; attempt++)
      {
         try
         {
            var page = await FetchOnceAsync(
               absoluteUrl,
               cancellationToken
            );

            return await AppendImageTextAsync(page, cancellationToken);
         }
         catch(OperationCanceledException)
            when(cancellationToken.IsCancellationRequested)
         {
            throw;
         }
         catch(Exception exception) when(
            IsTransientFailure(exception) &&
            attempt < MaxTransientRetryAttempts
         )
         {
            await DelayTransientRetryAsync(
               attempt,
               absoluteUrl,
               exception.Message,
               cancellationToken
            );
         }
      }

      return WebPageContentFetchSupport.BuildFailureContent(
         absoluteUrl,
         null,
         null,
         $"Could not retrieve page content from {absoluteUrl}.",
         "http"
      );
   }

   private async Task<WebPageContent?> AppendImageTextAsync(
      WebPageContent? page,
      CancellationToken cancellationToken
   )
   {
      if(page?.RelevantImages is not { Count: > 0 } images)
      {
         return page;
      }

      var imageText = await imageTextFetcher(images, cancellationToken);

      if(string.IsNullOrWhiteSpace(imageText))
      {
         return page;
      }

      var fullText = string.IsNullOrWhiteSpace(page.MainTextFull)
         ? imageText
         : page.MainTextFull.TrimEnd() +
            Environment.NewLine +
            Environment.NewLine +
            imageText;

      return page with
      {
         MainTextFull = fullText,
         MainText = WebPageContentFetchSupport.ApplyResponseCutoff(fullText),
         HasBodyText = true
      };
   }

   private async Task<WebPageContent?> FetchOnceAsync(
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var browserUserAgent = await this.browserUserAgentFetcher();
      using var request = new HttpRequestMessage(HttpMethod.Get, absoluteUrl);
      request.Headers.Accept.ParseAdd(
         "text/html,application/xhtml+xml,application/xml;q=0.9," +
         "image/avif,image/webp,*/*;q=0.8"
      );
      foreach(var header in WebPageContentFetchSupport.BuildBrowserLikeHeaders(
         browserUserAgent
      ))
      {
         request.Headers.TryAddWithoutValidation(
            header.Key,
            header.Value
         );
      }

      request.Headers.TryAddWithoutValidation("User-Agent", browserUserAgent);
      HttpResponseMessage response;

      try
      {
         response = await httpClient.SendAsync(
            request,
            cancellationToken
         );
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(Exception exception) when(
         exception is not TaskCanceledException &&
         exception is not TimeoutException
      )
      {
         logger.LogWarning(
            exception,
            "Primary HTTP request failed for {Url}; " +
            "trying browser renderer without HTTP fallback.",
            absoluteUrl
         );

         return await FetchRenderedPageWithoutPrimaryResponseAsync(
            absoluteUrl,
            cancellationToken,
            null
         );
      }
      catch(TaskCanceledException exception)
      {
         logger.LogWarning(
            exception,
            "Primary HTTP request timed out for {Url}; " +
            "trying browser renderer without HTTP fallback.",
            absoluteUrl
         );

         return await FetchRenderedPageWithoutPrimaryResponseAsync(
            absoluteUrl,
            cancellationToken,
            WebPageFetchErrorKind.Timeout
         );
      }
      catch(TimeoutException exception)
      {
         logger.LogWarning(
            exception,
            "Primary HTTP request timed out for {Url}; " +
            "trying browser renderer without HTTP fallback.",
            absoluteUrl
         );

         return await FetchRenderedPageWithoutPrimaryResponseAsync(
            absoluteUrl,
            cancellationToken,
            WebPageFetchErrorKind.Timeout
         );
      }

      using(response)
      {
         if(WebPageContentFetchSupport.IsPdfResponse(response, absoluteUrl))
         {
            return await WebPagePdfPageFetcher.FetchAsync(
               response,
               absoluteUrl,
               cancellationToken
            );
         }

         return await FetchRenderedHtmlWithPrimaryResponseAsync(
            response,
            absoluteUrl,
            cancellationToken
         );
      }
   }

   private async Task<WebPageContent?>
      FetchRenderedHtmlWithPrimaryResponseAsync(
      HttpResponseMessage response,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var primaryHtml = await response.Content.ReadAsStringAsync(
         cancellationToken
      );
      var primaryRelevantLinks =
         WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
            primaryHtml,
            absoluteUrl
         );
      var primaryRelevantImages =
         WebPageContentFetchSupport.ExtractRelevantImagesFromHtml(
            primaryHtml,
            absoluteUrl
         );
      var primaryHtmlContent = await WebPageHtmlPageFetcher.FetchHtmlAsync(
         NullLogger.Instance,
         static (_, _) => Task.FromResult<WebPageContent?>(null),
         primaryHtml,
         absoluteUrl,
         cancellationToken
      );

      if(IsRichContent(primaryHtmlContent))
      {
         logger.LogInformation(
            "Primary HTML response was sufficient for {Url}; " +
            "skipping browser renderer.",
            absoluteUrl
         );
         return MergePrimaryRelevantContent(
            primaryHtmlContent,
            primaryRelevantLinks,
            primaryRelevantImages
         );
      }

      try
      {
         var renderedContent = await this.browserPageFetcher(
            absoluteUrl,
            cancellationToken
         );

         if(HasFetchFailure(renderedContent))
         {
            logger.LogWarning(
               "Playwright renderer returned no usable content for {Url}; " +
               "using primary HTML response. Reason: {Reason}",
               absoluteUrl,
               renderedContent?.FetchErrorMessage ?? "No content returned."
            );
            return await FetchPrimaryHtmlFallbackAsync(
               primaryHtml,
               absoluteUrl,
               cancellationToken,
               renderedContent?.FetchErrorKind,
               renderedContent?.FetchErrorMessage,
               renderedContent?.BrowserStrategy,
               primaryRelevantLinks,
               primaryRelevantImages
            );
         }

         return MergePrimaryRelevantContent(
            renderedContent,
            primaryRelevantLinks,
            primaryRelevantImages
         );
      }
      catch(WebPageFetchException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright renderer failed for {Url} using strategy " +
            "{Strategy}; using primary HTML response.",
            absoluteUrl,
            exception.BrowserStrategy ?? "unknown"
         );
         return await FetchPrimaryHtmlFallbackAsync(
            primaryHtml,
            absoluteUrl,
            cancellationToken,
            exception.ErrorKind,
            exception.Message,
            exception.BrowserStrategy,
            primaryRelevantLinks,
            primaryRelevantImages
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
            exception,
            "Playwright renderer timed out for {Url}; " +
            "using primary HTML response.",
            absoluteUrl
         );
         return await FetchPrimaryHtmlFallbackAsync(
            primaryHtml,
            absoluteUrl,
            cancellationToken,
            WebPageFetchErrorKind.Timeout,
            exception.Message,
            null,
            primaryRelevantLinks,
            primaryRelevantImages
         );
      }
      catch(PlaywrightException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright renderer failed for {Url}; " +
            "using primary HTML response.",
            absoluteUrl
         );
         return await FetchPrimaryHtmlFallbackAsync(
            primaryHtml,
            absoluteUrl,
            cancellationToken,
            WebPageFetchErrorKind.BrowserBlocked,
            exception.Message,
            null,
            primaryRelevantLinks,
            primaryRelevantImages
         );
      }
   }

   private async Task<WebPageContent?> FetchPrimaryHtmlFallbackAsync(
      string primaryHtml,
      Uri absoluteUrl,
      CancellationToken cancellationToken,
      WebPageFetchErrorKind? browserFailureKind,
      string? browserFailureMessage,
      string? browserFailureStrategy,
      IReadOnlyList<WebPageRelevantLink> primaryRelevantLinks,
      IReadOnlyList<WebPageImageCandidate> primaryRelevantImages
   )
   {
      var htmlContent = await WebPageHtmlPageFetcher.FetchHtmlAsync(
         this.logger,
         this.curlPageFetcher,
         primaryHtml,
         absoluteUrl,
         cancellationToken,
         browserFailureKind
      );

      return MergePrimaryRelevantContent(
         AppendBrowserFailureMessage(
            htmlContent,
            browserFailureMessage,
            browserFailureStrategy
         ),
         primaryRelevantLinks,
         primaryRelevantImages
      );
   }

   private async Task<WebPageContent?>
      FetchRenderedPageWithoutPrimaryResponseAsync(
      Uri absoluteUrl,
      CancellationToken cancellationToken,
      WebPageFetchErrorKind? browserFailureKind
   )
   {
      string? browserFailureMessage = null;
      string? browserFailureStrategy = null;

      try
      {
         var renderedContent = await this.browserPageFetcher(
            absoluteUrl,
            cancellationToken
         );

         if(!HasFetchFailure(renderedContent))
         {
            return renderedContent;
         }

         browserFailureKind = renderedContent?.FetchErrorKind ??
            browserFailureKind;
         browserFailureMessage = renderedContent?.FetchErrorMessage;
         browserFailureStrategy = renderedContent?.BrowserStrategy;
         logger.LogWarning(
            "Playwright renderer returned no usable content for {Url}; " +
            "falling back to curl. Reason: {Reason}",
            absoluteUrl,
            browserFailureMessage ?? "No content returned."
         );
      }
      catch(WebPageFetchException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright failed for {Url} using strategy {Strategy}; " +
            "falling back to curl.",
            absoluteUrl,
            exception.BrowserStrategy ?? "unknown"
         );
         browserFailureKind = exception.ErrorKind;
         browserFailureMessage = exception.Message;
         browserFailureStrategy = exception.BrowserStrategy;
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(TimeoutException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright timed out for {Url}; falling back to curl.",
            absoluteUrl
         );
         browserFailureKind = WebPageFetchErrorKind.Timeout;
         browserFailureMessage = exception.Message;
      }
      catch(PlaywrightException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright failed for {Url}; falling back to curl.",
            absoluteUrl
         );
         browserFailureKind = WebPageFetchErrorKind.BrowserBlocked;
         browserFailureMessage = exception.Message;
      }

      try
      {
         var curlContent = await this.curlPageFetcher(
            absoluteUrl,
            cancellationToken
         );

         if(curlContent is not null)
         {
            return AppendBrowserFailureMessage(
               curlContent,
               browserFailureMessage,
               browserFailureStrategy
            );
         }
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
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

      var failureContent = WebPageContentFetchSupport.BuildFailureContent(
         absoluteUrl,
         null,
         browserFailureKind,
         $"Could not retrieve page content from {absoluteUrl}.",
         "curl",
         browserFailureStrategy
      );

      return AppendBrowserFailureMessage(
         failureContent,
         browserFailureMessage,
         browserFailureStrategy
      );
   }

   private static bool HasFetchFailure(WebPageContent? content)
   {
      return content is null ||
         content.FetchErrorKind is not null ||
         !string.IsNullOrWhiteSpace(content.FetchErrorMessage) ||
         (!content.HasBodyText &&
            content.RelevantImages is not { Count: > 0 });
   }

   private static bool IsRichContent(WebPageContent? content)
   {
      return !HasFetchFailure(content) &&
         content!.MainTextFull.Length >=
            WebPageFetchDefaults.RichContentMinimumCharacters;
   }

   private static WebPageContent? MergePrimaryRelevantContent(
      WebPageContent? renderedContent,
      IReadOnlyList<WebPageRelevantLink> primaryRelevantLinks,
      IReadOnlyList<WebPageImageCandidate> primaryRelevantImages
   )
   {
      if(renderedContent is null)
      {
         return renderedContent;
      }

      return renderedContent with
      {
         RelevantLinks = WebPageContentFetchSupport.MergeRelevantLinks(
            primaryRelevantLinks,
            renderedContent.RelevantLinks
         ),
         RelevantImages = MergeRelevantImages(
            primaryRelevantImages,
            renderedContent.RelevantImages
         )
      };
   }

   private static WebPageContent? AppendBrowserFailureMessage(
      WebPageContent? content,
      string? browserFailureMessage,
      string? browserFailureStrategy
   )
   {
      if(content is null)
      {
         return content;
      }

      var updatedContent = content;

      if(!string.IsNullOrWhiteSpace(content.FetchErrorMessage) &&
         !string.IsNullOrWhiteSpace(browserFailureMessage))
      {
         updatedContent = updatedContent with
         {
            FetchErrorMessage =
               $"{content.FetchErrorMessage} " +
               $"Playwright error: {browserFailureMessage}"
         };
      }

      if(!string.IsNullOrWhiteSpace(browserFailureStrategy))
      {
         updatedContent = updatedContent with
         {
            BrowserStrategy = browserFailureStrategy
         };
      }

      return updatedContent;
   }

   private static IReadOnlyList<WebPageImageCandidate> MergeRelevantImages(
      params IReadOnlyList<WebPageImageCandidate>?[] imageSets
   )
   {
      return imageSets
         .Where(images => images is not null)
         .SelectMany(images => images!)
         .DistinctBy(
            image => image.Url,
            StringComparer.OrdinalIgnoreCase
         )
         .Take(WebPageFetchDefaults.ImageOcrMaximumCandidateCount)
         .ToArray();
   }

   private static bool IsTransientFailure(Exception exception)
   {
      return exception is HttpRequestException;
   }

   private async Task DelayTransientRetryAsync(
      int attempt,
      Uri absoluteUrl,
      string reason,
      CancellationToken cancellationToken
   )
   {
      var delay = GetTransientRetryDelay(attempt);

      logger.LogWarning(
         "Web page fetch attempt {Attempt} failed for {Url} with {Reason}." +
         " Retrying in {Delay}.",
         attempt,
         absoluteUrl,
         reason,
         delay
      );

      await Task.Delay(delay, cancellationToken);
   }

   private static TimeSpan GetTransientRetryDelay(int attempt)
   {
      var retryDelays = WebPageFetchDefaults.TransientRetryDelays;

      if(attempt < 1 || attempt > retryDelays.Count)
      {
         return retryDelays[^1];
      }

      return retryDelays[attempt - 1];
   }
}
