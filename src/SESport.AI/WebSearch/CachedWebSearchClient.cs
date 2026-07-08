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
      int searchAttempt = 0
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
         SearxngSearchEngineRotation.GetEngineForAttempt(
            SearxngOptions.Engines,
            searchAttempt
         )
      );

      if(Cache.TryGet(cacheKey, out var cachedResponse))
      {
         return cachedResponse;
      }

      var response = await InnerClient.SearchAsync(
         normalizedQuery,
         maxResults,
         cancellationToken,
         searchAttempt
      );
      Cache.Store(cacheKey, response);
      return response;
   }
}
