using System.Net;
using System.Text;
using System.Text.Json;
using SESport.AI.ActivitySearch;

namespace SESport.Core.Tests.AIActivitySearch;

public class GroqChatActivitySearchClientTests
{
   [Fact]
   public async Task SearchPostsChatPayloadWithCompoundWebSearch()
   {
      var handler = new RecordingHandler(CreateGroqResponseJson());
      var httpClient = new HttpClient(handler);
      var client = new GroqChatActivitySearchClient(
         httpClient,
         new GroqChatActivitySearchClientOptions(
            new Uri("https://api.groq.com/openai/v1/"),
            "groq/compound-mini",
            "test-key"
         )
      );

      var result = await client.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      Assert.Equal(
         new Uri("https://api.groq.com/openai/v1/chat/completions"),
         handler.RequestUri
      );
      Assert.Equal("Bearer test-key", handler.AuthorizationHeader);
      Assert.Contains("\"model\":\"groq/compound-mini\"", handler.RequestBody);
      Assert.Contains("\"enabled_tools\":[\"web_search\"]", handler.RequestBody);
      Assert.Contains("\"country\":\"sweden\"", handler.RequestBody);
      Assert.Contains("Tre Kronor", handler.RequestBody);
      Assert.Contains("Tre Kronor vs Finland", result.RawContent);
      Assert.Equal("groq/compound-mini", result.Producer);
      Assert.Contains("Tre Kronor", result.Prompt);
      Assert.Single(result.Proposals);
   }

   [Fact]
   public async Task SearchRetriesMalformedSuccessfulResponseEnvelope()
   {
      var handler = new RecordingHandler(["\n\n", CreateGroqResponseJson()]);
      var httpClient = new HttpClient(handler);
      var client = new GroqChatActivitySearchClient(
         httpClient,
         new GroqChatActivitySearchClientOptions(
            new Uri("https://api.groq.com/openai/v1/"),
            "groq/compound-mini"
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
         """{"error":{"message":"Rate limit exceeded."}}""",
         HttpStatusCode.TooManyRequests
      );
      var httpClient = new HttpClient(handler);
      var client = new GroqChatActivitySearchClient(
         httpClient,
         new GroqChatActivitySearchClientOptions(
            new Uri("https://api.groq.com/openai/v1/"),
            "groq/compound-mini"
         )
      );

      var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
         client.SearchAsync(
            new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
            CancellationToken.None
         )
      );

      Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
      Assert.Contains("Rate limit exceeded.", exception.Message);
   }

   private static string CreateGroqResponseJson()
   {
      var content = """
      {
         "proposals": [
            {
               "title": "Tre Kronor vs Finland",
               "description": "A scheduled match.",
               "activityType": "Match",
               "activityDate": "2026-06-01",
               "localStartTime": "19:00",
               "timeZoneId": "Europe/Stockholm",
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
         model = "groq/compound-mini",
         choices = new[]
         {
            new
            {
               message = new
               {
                  role = "assistant",
                  content
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

      public string? AuthorizationHeader { get; private set; }

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
         AuthorizationHeader = request.Headers.Authorization?.ToString();
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
