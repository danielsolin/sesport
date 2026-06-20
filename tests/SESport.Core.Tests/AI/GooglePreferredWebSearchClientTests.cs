using SESport.AI.Interfaces;
using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public sealed class GooglePreferredWebSearchClientTests
{
   [Fact]
   public async Task SearchUsesGoogleResultsWhenAvailable()
   {
      var client = new GooglePreferredWebSearchClient(
         new FakeWebSearchClient(
            [
               new(
                  "Google result",
                  "https://example.test/google",
                  null,
                  null
               )
            ]
         ),
         new FakeWebSearchClient(
            [
               new(
                  "Fallback result",
                  "https://example.test/fallback",
                  null,
                  null
               )
            ]
         )
      );

      var results = await client.SearchAsync(
         "query",
         5,
         CancellationToken.None
      );

      Assert.Single(results);
      Assert.Equal("https://example.test/google", results[0].Url);
   }

   [Fact]
   public async Task SearchFallsBackWhenGoogleReturnsNoResults()
   {
      var client = new GooglePreferredWebSearchClient(
         new FakeWebSearchClient([]),
         new FakeWebSearchClient(
            [
               new(
                  "Fallback result",
                  "https://example.test/fallback",
                  null,
                  null
               )
            ]
         )
      );

      var results = await client.SearchAsync(
         "query",
         5,
         CancellationToken.None
      );

      Assert.Single(results);
      Assert.Equal("https://example.test/fallback", results[0].Url);
   }

   [Fact]
   public async Task SearchFallsBackWhenGoogleThrows()
   {
      var client = new GooglePreferredWebSearchClient(
         new ThrowingWebSearchClient(),
         new FakeWebSearchClient(
            [
               new(
                  "Fallback result",
                  "https://example.test/fallback",
                  null,
                  null
               )
            ]
         )
      );

      var results = await client.SearchAsync(
         "query",
         5,
         CancellationToken.None
      );

      Assert.Single(results);
      Assert.Equal("https://example.test/fallback", results[0].Url);
   }

   private sealed class FakeWebSearchClient : IWebSearchClient
   {
      private readonly IReadOnlyList<WebSearchResult> results;

      public FakeWebSearchClient(IReadOnlyList<WebSearchResult> results)
      {
         this.results = results;
      }

      public Task<IReadOnlyList<WebSearchResult>> SearchAsync(
         string query,
         int maxResults,
         CancellationToken cancellationToken
      )
      {
         _ = query;
         _ = maxResults;
         _ = cancellationToken;
         return Task.FromResult(results);
      }
   }

   private sealed class ThrowingWebSearchClient : IWebSearchClient
   {
      public Task<IReadOnlyList<WebSearchResult>> SearchAsync(
         string query,
         int maxResults,
         CancellationToken cancellationToken
      )
      {
         _ = query;
         _ = maxResults;
         _ = cancellationToken;
         throw new InvalidOperationException("Google robot detection");
      }
   }
}
