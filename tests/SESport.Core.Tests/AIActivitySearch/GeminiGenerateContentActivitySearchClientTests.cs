using System.Net;
using System.Text;
using System.Text.Json;
using SESport.AI.ActivitySearch;

namespace SESport.Core.Tests.AIActivitySearch;

public class GeminiGenerateContentActivitySearchClientTests
{
   [Fact]
   public async Task SearchPostsGenerateContentPayloadWithGoogleSearchTool()
   {
      var handler = new RecordingHandler(CreateGeminiResponseJson());
      var httpClient = new HttpClient(handler);
      var client = new GeminiGenerateContentActivitySearchClient(
         httpClient,
         new GeminiGenerateContentActivitySearchClientOptions(
            new Uri("https://generativelanguage.googleapis.com/v1beta/"),
            "gemini-3.5-flash",
            "test-key"
         )
      );

      var result = await client.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      Assert.Equal(
         new Uri(
            "https://generativelanguage.googleapis.com/v1beta/" +
            "models/gemini-3.5-flash:generateContent"
         ),
         handler.RequestUri
      );
      Assert.Equal("test-key", handler.GoogleApiKeyHeader);
      Assert.Contains("\"google_search\":{}", handler.RequestBody);
      Assert.Contains("Tre Kronor", handler.RequestBody);
      Assert.Contains("Tre Kronor vs Finland", result.RawContent);
      Assert.Equal("gemini/gemini-3.5-flash", result.Producer);
      Assert.Contains("Tre Kronor", result.Prompt);
      Assert.Single(result.Proposals);
   }

   [Fact]
   public async Task SearchRetriesMalformedSuccessfulResponseEnvelope()
   {
      var handler = new RecordingHandler(
         ["\n\n", CreateGeminiResponseJson()]
      );
      var httpClient = new HttpClient(handler);
      var client = new GeminiGenerateContentActivitySearchClient(
         httpClient,
         new GeminiGenerateContentActivitySearchClientOptions(
            new Uri("https://generativelanguage.googleapis.com/v1beta/"),
            "gemini-3.5-flash"
         )
      );

      var result = await client.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestCount);
      Assert.Single(result.Proposals);
   }

   [Fact]
   public async Task SearchExceptionKeepsHttpStatusCode()
   {
      var handler = new RecordingHandler(
         """{"error":{"message":"Quota exceeded."}}""",
         HttpStatusCode.TooManyRequests
      );
      var httpClient = new HttpClient(handler);
      var client = new GeminiGenerateContentActivitySearchClient(
         httpClient,
         new GeminiGenerateContentActivitySearchClientOptions(
            new Uri("https://generativelanguage.googleapis.com/v1beta/"),
            "gemini-3.5-flash"
         )
      );

      var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
         client.SearchAsync(
            new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
            CancellationToken.None
         )
      );

      Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
      Assert.Contains("Quota exceeded.", exception.Message);
   }

   private static string CreateGeminiResponseJson()
   {
      var content = $$"""
      {
         "proposals": [
            {
               "title": "Tre Kronor vs Finland",
               "description": "A scheduled match.",
               "activityType": "Match",
               "activityDate": "2026-06-01",
               "localStartTime": "19:00",
               "timeZoneId": "{{SportDay.TimeZoneId}}",
               "context": "International friendly",
               "entityRole": "CompetesIn",
               "entityExplanation": "Tre Kronor participates.",
               "confidence": 0.8,
               "evidence": [
                  {
                     "sourceName": "Swehockey",
                     "uri": "https://example.test/game",
                     "title": "Schedule",
                     "summary": "The source lists the match."
                  }
               ]
            }
         ]
      }
      """;
      var payload = new
      {
         candidates = new[]
         {
            new
            {
               content = new
               {
                  role = "model",
                  parts = new[]
                  {
                     new { text = content }
                  }
               }
            }
         }
      };

      return JsonSerializer.Serialize(payload);
   }

   private static ActivitySearchEntity CreateEntity()
   {
      return new ActivitySearchEntity(
         new ExternalEntityId("tre-kronor"),
         "Tre Kronor",
         "national_team",
         new ImportedSport(new ExternalEntityId("ice-hockey"), "ice hockey"),
         "Represents Sweden",
         "Current Swedish men's national ice hockey team.",
         ["championships", "roster announcements"],
         "Swehockey",
         "Strong long-term watchlist anchor"
      );
   }

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly Queue<string> responseJsons;
      private readonly HttpStatusCode statusCode;

      public RecordingHandler(
         string responseJson,
         HttpStatusCode statusCode = HttpStatusCode.OK
      )
         : this([responseJson], statusCode)
      {
      }

      public RecordingHandler(
         IEnumerable<string> responseJsons,
         HttpStatusCode statusCode = HttpStatusCode.OK
      )
      {
         this.responseJsons = new Queue<string>(responseJsons);
         this.statusCode = statusCode;
      }

      public Uri? RequestUri { get; private set; }

      public string RequestBody { get; private set; } = "";

      public string? GoogleApiKeyHeader { get; private set; }

      public int RequestCount { get; private set; }

      protected override async Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestCount++;
         RequestUri = request.RequestUri;
         RequestBody = request.Content is null
            ? ""
            : await request.Content.ReadAsStringAsync(cancellationToken);
         GoogleApiKeyHeader = request.Headers.TryGetValues(
            "x-goog-api-key",
            out var values
         )
            ? values.SingleOrDefault()
            : null;
         var responseJson = responseJsons.Count > 1
            ? responseJsons.Dequeue()
            : responseJsons.Peek();

         return new HttpResponseMessage(statusCode)
         {
            Content = new StringContent(
               responseJson,
               Encoding.UTF8,
               "application/json"
            )
         };
      }
   }
}
