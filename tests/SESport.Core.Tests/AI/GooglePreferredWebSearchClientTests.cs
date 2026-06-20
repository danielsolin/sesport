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
            ],
            "Google"
         ),
         new FakeWebSearchClient(
            [
               new(
                  "Fallback result",
                  "https://example.test/fallback",
                  null,
                  null
               )
            ],
            "SearXNG"
         )
      );

      var response = await client.SearchAsync(
         "query",
         5,
         CancellationToken.None
      );

      Assert.Single(response.Results);
      Assert.Equal("https://example.test/google", response.Results[0].Url);
      Assert.Equal("Google", response.Provider);
   }

   [Fact]
   public async Task SearchFallsBackWhenGoogleReturnsNoResults()
   {
      var client = new GooglePreferredWebSearchClient(
         new FakeWebSearchClient([], "Google"),
         new FakeWebSearchClient(
            [
               new(
                  "Fallback result",
                  "https://example.test/fallback",
                  null,
                  null
               )
            ],
            "SearXNG"
         )
      );

      var response = await client.SearchAsync(
         "query",
         5,
         CancellationToken.None
      );

      Assert.Single(response.Results);
      Assert.Equal("https://example.test/fallback", response.Results[0].Url);
      Assert.Equal("Google -> SearXNG fallback", response.Provider);
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
            ],
            "SearXNG"
         )
      );

      var response = await client.SearchAsync(
         "query",
         5,
         CancellationToken.None
      );

      Assert.Single(response.Results);
      Assert.Equal("https://example.test/fallback", response.Results[0].Url);
      Assert.Equal("Google -> SearXNG fallback", response.Provider);
   }

   private sealed class FakeWebSearchClient : IWebSearchClient
   {
      private readonly IReadOnlyList<WebSearchResult> results;
      private readonly string? provider;

      public FakeWebSearchClient(
         IReadOnlyList<WebSearchResult> results,
         string? provider = null
      )
      {
         this.results = results;
         this.provider = provider;
      }

      public Task<WebSearchResponse> SearchAsync(
         string query,
         int maxResults,
         CancellationToken cancellationToken
      )
      {
         _ = query;
         _ = maxResults;
         _ = cancellationToken;
         return Task.FromResult(new WebSearchResponse(results, provider));
      }
   }

   private sealed class ThrowingWebSearchClient : IWebSearchClient
   {
      public Task<WebSearchResponse> SearchAsync(
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
