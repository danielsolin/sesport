using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public class GoogleWebSearchClientTests
{
   [Fact]
   public async Task SearchBuildsGoogleUri()
   {
      Uri? capturedUri = null;
      int capturedMaxResults = 0;

      var client = new GoogleWebSearchClient(
         new HttpClient(),
         (uri, maxResults, _) =>
         {
            capturedUri = uri;
            capturedMaxResults = maxResults;
            return Task.FromResult(
               new GoogleWebSearchClient.GoogleSearchAttempt(
                  [
                     new(
                        "The Amateur Championship START LIST FOR ROUND 1",
                        "https://assets.randa.org/c42c7bf4-dca7-00ea-4f2e-" +
                        "373223f80f76/2542a870-e7bb-4d86-a914-2863ef412282/" +
                        "MP%20Round%201%20Draw.pdf",
                        null,
                        null
                     )
                  ]
               )
            );
         }
      );

      var response = await client.SearchAsync(
         "R&A The Amateur Championship 2026 entry list Day 1 first session",
         5,
         CancellationToken.None
      );

      Assert.NotNull(capturedUri);
      Assert.Equal("https", capturedUri!.Scheme);
      Assert.Equal("www.google.com", capturedUri.Host);
      Assert.Equal("/search", capturedUri.AbsolutePath);
      Assert.Contains("q=R%26A+The+Amateur+Championship+2026+entry+list+" +
         "Day+1+first+session", capturedUri.Query);
      Assert.Contains("hl=en", capturedUri.Query);
      Assert.Contains("gl=us", capturedUri.Query);
      Assert.Contains("pws=0", capturedUri.Query);
      Assert.Contains("num=5", capturedUri.Query);
      Assert.Equal(5, capturedMaxResults);
      Assert.Single(response.Results);
      Assert.Equal(
         "https://assets.randa.org/c42c7bf4-dca7-00ea-4f2e-" +
         "373223f80f76/2542a870-e7bb-4d86-a914-2863ef412282/" +
         "MP%20Round%201%20Draw.pdf",
         response.Results[0].Url
      );
   }

   [Fact]
   public async Task EmptyQuerySkipsFetcher()
   {
      var called = false;

      var client = new GoogleWebSearchClient(
         new HttpClient(),
         (_, _, _) =>
         {
            called = true;
            return Task.FromResult(
               new GoogleWebSearchClient.GoogleSearchAttempt([])
            );
         }
      );

      var response = await client.SearchAsync(
         " ",
         3,
         CancellationToken.None
      );

      Assert.Empty(response.Results);
      Assert.False(called);
   }
}
