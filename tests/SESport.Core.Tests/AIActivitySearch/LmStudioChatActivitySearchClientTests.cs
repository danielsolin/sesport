using System.Net;
using System.Text;
using System.Text.Json;
using SESport.AI.ActivitySearch;
using SESport.Core.Ingestion;

namespace SESport.Core.Tests.AIActivitySearch;

public class LmStudioChatActivitySearchClientTests
{
   [Fact]
   public async Task SearchPostsChatPayloadWithPluginIntegration()
   {
      var handler = new RecordingHandler(CreateChatResponseJson());
      var httpClient = new HttpClient(handler);
      var client = new LmStudioChatActivitySearchClient(
         httpClient,
         new LmStudioChatActivitySearchClientOptions(
            new Uri("http://127.0.0.1:1234/api/v1/"),
            "gpt-oss-20b",
            "altra/web-search",
            ["search"]
         )
      );

      var result = await client.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      Assert.Equal(
         new Uri("http://127.0.0.1:1234/api/v1/chat"),
         handler.RequestUri
      );
      Assert.Contains("\"model\":\"gpt-oss-20b\"", handler.RequestBody);
      Assert.Contains("\"type\":\"plugin\"", handler.RequestBody);
      Assert.Contains("\"id\":\"altra/web-search\"", handler.RequestBody);
      Assert.Contains("\"allowed_tools\":[\"search\"]", handler.RequestBody);
      Assert.Single(result.Proposals);
      Assert.Contains("Tre Kronor vs Finland", result.RawContent);
      Assert.Equal("lmstudio/gpt-oss-20b", result.Producer);
      Assert.Contains("Tre Kronor", result.Prompt);
      Assert.Contains("Tre Kronor", handler.RequestBody);
   }

   [Fact]
   public async Task SearchExceptionKeepsHttpStatusCode()
   {
      var handler = new RecordingHandler(
         "Provider credits are exhausted.",
         HttpStatusCode.PaymentRequired
      );
      var httpClient = new HttpClient(handler);
      var client = new LmStudioChatActivitySearchClient(
         httpClient,
         new LmStudioChatActivitySearchClientOptions(
            new Uri("http://127.0.0.1:1234/api/v1/"),
            "gpt-oss-20b",
            "altra/web-search",
            ["search"]
         )
      );

      var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
         client.SearchAsync(
            new ActivitySearchRequest(
               CreateEntity(),
               new DateOnly(2026, 5, 31)
            ),
            CancellationToken.None
         )
      );

      Assert.Equal(HttpStatusCode.PaymentRequired, exception.StatusCode);
   }

   private static string CreateChatResponseJson()
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
         output = new object[]
         {
            new
            {
               type = "tool_call",
               tool = "search",
               output = "Search results"
            },
            new
            {
               type = "message",
               content
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

   private sealed class RecordingHandler(
      string responseJson,
      HttpStatusCode statusCode = HttpStatusCode.OK
   )
      : HttpMessageHandler
   {
      public Uri? RequestUri { get; private set; }

      public string RequestBody { get; private set; } = "";

      protected override async Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestUri = request.RequestUri;
         RequestBody = request.Content is null
            ? ""
            : await request.Content.ReadAsStringAsync(cancellationToken);

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
