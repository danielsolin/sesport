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
   public async Task SearchStoresFallbackResponseUnderActualEngine()
   {
      var innerClient = new RecordingWebSearchClient(
         "SearXNG/brave",
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/roster",
            null
         )
      );
      var client = CreateClient(
         innerClient,
         new SearxngWebSearchClientOptions
         {
            Engines =
            [
               "bing",
               "brave"
            ]
         }
      );

      await client.SearchAsync("Tre Kronor", 3, CancellationToken.None, 0);
      await client.SearchAsync("Tre Kronor", 3, CancellationToken.None, 1);

      Assert.Single(innerClient.Queries);
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
      RecordingWebSearchClient innerClient,
      SearxngWebSearchClientOptions? options = null
   )
   {
      return new CachedWebSearchClient(
         innerClient,
         new WebSearchCache(),
         options ?? new SearxngWebSearchClientOptions()
      );
   }

   private sealed class RecordingWebSearchClient : IWebSearchClient
   {
      private readonly IReadOnlyList<WebSearchResult> results;
      private readonly string? provider;

      public RecordingWebSearchClient(params WebSearchResult[] results)
         : this(null, results)
      {
      }

      public RecordingWebSearchClient(
         string? provider,
         params WebSearchResult[] results
      )
      {
         this.results = results;
         this.provider = provider;
      }

      public List<string> Queries { get; } = [];

      public Task<WebSearchResponse> SearchAsync(
         string query,
         int maxResults,
         CancellationToken cancellationToken,
         int searchAttempt = 0,
         bool includeSocialMedia = false
      )
      {
         _ = maxResults;
         _ = cancellationToken;
         _ = searchAttempt;
         _ = includeSocialMedia;
         Queries.Add(query);
         return Task.FromResult(new WebSearchResponse(results, provider));
      }
   }
}
