using SESport.AI.Interfaces;

namespace SESport.AI.WebSearch;

public sealed class CachedWebSearchClient : IWebSearchClient
{
   public CachedWebSearchClient(
      IWebSearchClient innerClient,
      WebSearchCache cache,
      SearxngWebSearchClientOptions searxngOptions
   )
   {
      InnerClient = innerClient;
      Cache = cache;
      SearxngOptions = searxngOptions;
   }

   private IWebSearchClient InnerClient { get; }

   private WebSearchCache Cache { get; }

   private SearxngWebSearchClientOptions SearxngOptions { get; }

   public async Task<WebSearchResponse> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken,
      int searchAttempt = 0,
      bool includeSocialMedia = false
   )
   {
      var normalizedQuery = query.Trim();

      if(string.IsNullOrWhiteSpace(normalizedQuery))
      {
         return new WebSearchResponse([]);
      }

      var cacheKey = new WebSearchCacheKey(
         normalizedQuery,
         maxResults,
         GetRequestedEngine(searchAttempt),
         includeSocialMedia
      );

      if(Cache.TryGet(cacheKey, out var cachedResponse))
      {
         return cachedResponse;
      }

      var response = await InnerClient.SearchAsync(
         normalizedQuery,
         maxResults,
         cancellationToken,
         searchAttempt,
         includeSocialMedia
      );
      var cacheEngine = ResolveCacheEngine(
         response,
         cacheKey.Engine
      );

      Cache.Store(
         new WebSearchCacheKey(
            normalizedQuery,
            maxResults,
            cacheEngine,
            includeSocialMedia
         ),
         response
      );
      return response;
   }

   private string GetRequestedEngine(int searchAttempt)
   {
      return SearxngSearchEngineRotation.GetEngineForAttempt(
         SearxngOptions.Engines,
         searchAttempt
      );
   }

   private static string ResolveCacheEngine(
      WebSearchResponse response,
      string requestedEngine
   )
   {
      return TryGetEngineFromValue(response.Provider, "SearXNG/") ??
         TryGetEngineFromValue(response.Details, "engines=") ??
         requestedEngine;
   }

   private static string? TryGetEngineFromValue(
      string? value,
      string prefix
   )
   {
      if(string.IsNullOrWhiteSpace(value) ||
         !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
      {
         return null;
      }

      var engine = value[prefix.Length..].Trim();
      return string.IsNullOrWhiteSpace(engine) ? null : engine;
   }
}
