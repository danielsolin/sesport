using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

using SESport.AI.Interfaces;

namespace SESport.AI.WebPages;

public sealed class WebPageContentClient : IWebPageContentClient
{
   private const int MaxTransientRetryAttempts = 3;
   internal const string CutoffMarker =
      WebPageContentFetchSupport.CutoffMarker;
   internal const int MaxResponseCharacters =
      WebPageContentFetchSupport.MaxResponseCharacters;

   private readonly HttpClient httpClient;
   private readonly ILogger<WebPageContentClient> logger;
   private readonly Func<Task<string>> browserUserAgentFetcher;
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      browserPageFetcher;
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      curlPageFetcher;

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
         null
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

      return await FetchWithRetryAsync(
         absoluteUrl,
         cancellationToken
      );
   }

   internal static string BuildBrowserUserAgent(string browserVersion)
   {
      return WebPageContentFetchSupport.BuildBrowserUserAgent(browserVersion);
   }

   internal static string ApplyResponseCutoff(string text)
   {
      return WebPageContentFetchSupport.ApplyResponseCutoff(text);
   }

   internal static string? GetCountryDisplayName(string? countryCode)
   {
      return WebPageContentFetchSupport.GetCountryDisplayName(countryCode);
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
            return await FetchOnceAsync(
               absoluteUrl,
               cancellationToken
            );
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
      try
      {
         return await this.browserPageFetcher(
            absoluteUrl,
            cancellationToken
         );
      }
      catch(WebPageFetchException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright renderer failed for {Url}; " +
            "using primary HTML response.",
            absoluteUrl
         );
         return await WebPageHtmlPageFetcher.FetchAsync(
            this.logger,
            this.curlPageFetcher,
            response,
            absoluteUrl,
            cancellationToken,
            exception.ErrorKind
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
         return await WebPageHtmlPageFetcher.FetchAsync(
            this.logger,
            this.curlPageFetcher,
            response,
            absoluteUrl,
            cancellationToken,
            WebPageFetchErrorKind.Timeout
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
         return await WebPageHtmlPageFetcher.FetchAsync(
            this.logger,
            this.curlPageFetcher,
            response,
            absoluteUrl,
            cancellationToken,
            WebPageFetchErrorKind.BrowserBlocked
         );
      }
   }

   private async Task<WebPageContent?>
      FetchRenderedPageWithoutPrimaryResponseAsync(
      Uri absoluteUrl,
      CancellationToken cancellationToken,
      WebPageFetchErrorKind? browserFailureKind
   )
   {
      try
      {
         return await this.browserPageFetcher(absoluteUrl, cancellationToken);
      }
      catch(WebPageFetchException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright failed for {Url}; falling back to curl.",
            absoluteUrl
         );
         browserFailureKind = exception.ErrorKind;
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
      }
      catch(PlaywrightException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright failed for {Url}; falling back to curl.",
            absoluteUrl
         );
         browserFailureKind = WebPageFetchErrorKind.BrowserBlocked;
      }

      try
      {
         var curlContent = await this.curlPageFetcher(
            absoluteUrl,
            cancellationToken
         );

         if(curlContent is not null)
         {
            return curlContent;
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

      return WebPageContentFetchSupport.BuildFailureContent(
         absoluteUrl,
         null,
         browserFailureKind,
         $"Could not retrieve page content from {absoluteUrl}.",
         "curl"
      );
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
      return attempt switch
      {
         1 => TimeSpan.FromSeconds(2),
         2 => TimeSpan.FromSeconds(5),
         _ => TimeSpan.FromSeconds(10)
      };
   }
}
