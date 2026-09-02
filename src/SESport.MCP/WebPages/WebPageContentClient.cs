using Microsoft.Extensions.Logging.Abstractions;

namespace SESport.AI.WebPages;

public sealed class WebPageContentClient : IWebPageContentClient
{
   private readonly WebPageFetchOrchestrator _orchestrator;
   private readonly WebPageContentCache? _pageCache;

   [ActivatorUtilitiesConstructor]
   public WebPageContentClient(
      HttpClient httpClient,
      ILogger<WebPageContentClient>? logger = null,
      WebPageContentCache? pageCache = null
   )
      : this(httpClient, null, logger, null, null, null, pageCache)
   {
   }

   internal WebPageContentClient(
      HttpClient httpClient,
      Func<Uri, IReadOnlyList<BrowserStrategyDescriptor>,
         CancellationToken, Task<WebPageBrowserOutcome>>? browserFetcher,
      ILogger<WebPageContentClient>? logger,
      Func<Task<string>>? browserUserAgentFetcher,
      Func<Uri, int, CancellationToken,
         Task<WebPageHttpResponse>>? curlTransport,
      Func<IReadOnlyList<WebPageImageCandidate>,
         CancellationToken, Task<string>>? imageTextFetcher,
      WebPageContentCache? pageCache = null
   )
   {
      var effectiveLogger = logger ??
         NullLogger<WebPageContentClient>.Instance;

      var effectiveBrowserUserAgentFetcher = browserUserAgentFetcher ??
         WebPageContentFetchSupport.GetBrowserUserAgentAsync;

      var effectiveBrowserFetcher = browserFetcher ??
         ((uri, strategies, cancellationToken) =>
            WebPageBrowserPageFetcher.FetchAsync(
               effectiveLogger,
               effectiveBrowserUserAgentFetcher,
               uri,
               strategies,
               cancellationToken
            ));

      var effectiveCurlTransport = curlTransport ??
         ((uri, maxTimeSeconds, cancellationToken) =>
            WebPageCurlTransport.SendAsync(
               uri,
               maxTimeSeconds,
               cancellationToken
            ));

      var effectiveImageTextFetcher = imageTextFetcher ??
         ((images, cancellationToken) =>
            WebPageImageOcr.ExtractAsync(
               httpClient,
               effectiveLogger,
               images,
               cancellationToken
            ));

      _orchestrator = new WebPageFetchOrchestrator(
         httpClient,
         effectiveLogger,
         effectiveBrowserUserAgentFetcher,
         effectiveBrowserFetcher,
         effectiveCurlTransport,
         effectiveImageTextFetcher
      );
      _pageCache = pageCache;
   }

   public Task<WebPageContent?> FetchAsync(
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
         return Task.FromResult<WebPageContent?>(null);
      }

      if(_pageCache is null)
      {
         return _orchestrator.FetchAsync(absoluteUrl, cancellationToken);
      }

      return _pageCache.GetOrFetchAsync(
         absoluteUrl,
         cancellationToken,
         token => _orchestrator.FetchAsync(absoluteUrl, token)
      );
   }
}
