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
      var googleResponse = await TrySearchAsync(
         GoogleWebSearchClient,
         query,
         maxResults,
         cancellationToken
      );

      if(googleResponse.Results.Count > 0)
      {
         return googleResponse;
      }

      var searxngResponse = await TrySearchAsync(
         SearxngWebSearchClient,
         query,
         maxResults,
         cancellationToken
      );
      return new WebSearchResponse(
         searxngResponse.Results,
         "Google -> SearXNG fallback",
         googleResponse.Details is not null
            ? $"Google failed: {googleResponse.Details}"
            : "Google returned no results"
      );
   }

   private static async Task<WebSearchResponse> TrySearchAsync(
      IWebSearchClient client,
      string query,
      int maxResults,
      CancellationToken cancellationToken
   )
   {
      try
      {
         return await client.SearchAsync(
            query,
            maxResults,
            cancellationToken
         );
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch
      {
         return new WebSearchResponse([]);
      }
   }
}
