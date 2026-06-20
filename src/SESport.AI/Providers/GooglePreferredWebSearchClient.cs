using SESport.AI.Interfaces;

namespace SESport.AI.Providers;

public sealed class GooglePreferredWebSearchClient : IWebSearchClient
{
   public GooglePreferredWebSearchClient(
      IWebSearchClient googleWebSearchClient,
      IWebSearchClient searxngWebSearchClient
   )
   {
      GoogleWebSearchClient = googleWebSearchClient;
      SearxngWebSearchClient = searxngWebSearchClient;
   }

   private IWebSearchClient GoogleWebSearchClient { get; }

   private IWebSearchClient SearxngWebSearchClient { get; }

   public async Task<WebSearchResponse> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken
   )
   {
      var googleResults = await TrySearchAsync(
         GoogleWebSearchClient,
         query,
         maxResults,
         cancellationToken
      );

      if(googleResults.Count > 0)
      {
         return new WebSearchResponse(googleResults, "Google");
      }

      var searxngResults = await TrySearchAsync(
         SearxngWebSearchClient,
         query,
         maxResults,
         cancellationToken
      );
      return new WebSearchResponse(
         searxngResults,
         "Google -> SearXNG fallback"
      );
   }

   private static async Task<IReadOnlyList<WebSearchResult>> TrySearchAsync(
      IWebSearchClient client,
      string query,
      int maxResults,
      CancellationToken cancellationToken
   )
   {
      try
      {
         return (await client.SearchAsync(
            query,
            maxResults,
            cancellationToken
         )).Results;
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch
      {
         return [];
      }
   }
}
