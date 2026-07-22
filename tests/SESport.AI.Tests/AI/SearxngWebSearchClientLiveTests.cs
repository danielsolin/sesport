using SESport.AI.WebSearch;
using SESport.Core.Configuration;

namespace SESport.Core.Tests.AI;

public sealed class SearxngWebSearchClientLiveTests
{
   [Fact]
   public async Task SearchRecentUsesConfiguredLocalSearxng()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = new HttpClient
      {
         Timeout = TimeSpan.FromMinutes(3)
      };
      var client = new SearxngWebSearchClient(
         httpClient,
         new SearxngWebSearchClientOptions
         {
            BaseUrl = Environment.GetEnvironmentVariable(
               "SearXNG__BaseUrl"
            ) ?? SearxngWebSearchClientOptions.DefaultBaseUrl
         }
      );

      var response = await client.SearchRecentAsync(
         "William Nylander",
         10,
         CancellationToken.None
      );

      Assert.NotEmpty(response.Results);

      foreach(var result in response.Results)
      {
         Console.WriteLine(
            $"{result.PublishedAt:O}\t{result.Title}\t{result.Url}"
         );
      }

      Console.WriteLine($"Provider: {response.Provider}");
      Console.WriteLine($"Details: {response.Details}");
   }

   private static bool ShouldRunLiveTest()
   {
      return string.Equals(
         Environment.GetEnvironmentVariable(
            "SESPORT_RUN_LIVE_SEARXNG_TESTS"
         ),
         "1",
         StringComparison.OrdinalIgnoreCase
      );
   }
}
