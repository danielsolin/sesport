using SESport.AI.Interfaces;
using SESport.AI.WebSearch;

namespace SESport.Core.Tests.AI;

public sealed class CachedWebSearchClientTests
{
   [Fact]
   public async Task SearchReusesExactQueryEngineAndLimit()
   {
      var innerClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/roster",
            null
         )
      );
      var client = CreateClient(innerClient);

      await client.SearchAsync("Tre Kronor", 3, CancellationToken.None, 0);
      await client.SearchAsync("Tre Kronor", 3, CancellationToken.None, 0);

      Assert.Single(innerClient.Queries);
   }

   [Fact]
   public async Task SearchDoesNotReuseSimilarQueries()
   {
      var innerClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/roster",
            null
         )
      );
      var client = CreateClient(innerClient);

      await client.SearchAsync("Tre Kronor", 3, CancellationToken.None, 0);
      await client.SearchAsync("tre kronor", 3, CancellationToken.None, 0);

      Assert.Equal(2, innerClient.Queries.Count);
   }

   [Fact]
   public async Task SearchDoesNotReuseDifferentEngines()
   {
      var innerClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/roster",
            null
         )
      );
      var client = CreateClient(innerClient);

      await client.SearchAsync("Tre Kronor", 3, CancellationToken.None, 0);
      await client.SearchAsync("Tre Kronor", 3, CancellationToken.None, 1);

      Assert.Equal(2, innerClient.Queries.Count);
   }

   [Fact]
   public async Task SearchReusesEmptyResults()
   {
      var innerClient = new RecordingWebSearchClient();
      var client = CreateClient(innerClient);

      await client.SearchAsync("Tre Kronor", 3, CancellationToken.None, 0);
      await client.SearchAsync("Tre Kronor", 3, CancellationToken.None, 0);

      Assert.Single(innerClient.Queries);
   }

   private static CachedWebSearchClient CreateClient(
      RecordingWebSearchClient innerClient
   )
   {
      return new CachedWebSearchClient(
         innerClient,
         new WebSearchCache(),
         new SearxngWebSearchClientOptions()
      );
   }

   private sealed class RecordingWebSearchClient : IWebSearchClient
   {
      private readonly IReadOnlyList<WebSearchResult> results;

      public RecordingWebSearchClient(params WebSearchResult[] results)
      {
         this.results = results;
      }

      public List<string> Queries { get; } = [];

      public Task<WebSearchResponse> SearchAsync(
         string query,
         int maxResults,
         CancellationToken cancellationToken,
         int searchAttempt = 0
      )
      {
         _ = maxResults;
         _ = cancellationToken;
         _ = searchAttempt;
         Queries.Add(query);
         return Task.FromResult(new WebSearchResponse(results));
      }
   }
}
