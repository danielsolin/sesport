using System.Net;
using System.Text;
using System.Text.Json;
using SESport.Core.AIActivitySearch;
using SESport.Core.Ingestion;

namespace SESport.Core.Tests.AIActivitySearch;

public class OpenAiResponsesActivitySearchClientTests
{
   [Fact]
   public async Task SearchPostsResponsesPayloadWithWebSearchTool()
   {
      var handler = new RecordingHandler(
         CreateResponseJson(includeReasoning: true)
      );
      var httpClient = new HttpClient(handler);
      var client = new OpenAiResponsesActivitySearchClient(
         httpClient,
         new OpenAiResponsesActivitySearchClientOptions(
            new Uri("http://127.0.0.1:1234/v1/"),
            "gpt-oss-20b"
         )
      );

      var result = await client.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      Assert.Equal(
         new Uri("http://127.0.0.1:1234/v1/responses"),
         handler.RequestUri
      );
      Assert.Contains("\"model\":\"gpt-oss-20b\"", handler.RequestBody);
      Assert.Contains("\"type\":\"web_search\"", handler.RequestBody);
      Assert.Contains("Tre Kronor vs Finland", result.RawContent);
      Assert.Single(result.Proposals);
   }

   [Fact]
   public async Task SearchCanOmitWebSearchTool()
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var httpClient = new HttpClient(handler);
      var client = new OpenAiResponsesActivitySearchClient(
         httpClient,
         new OpenAiResponsesActivitySearchClientOptions(
            new Uri("http://127.0.0.1:1234/v1/"),
            "gpt-oss-20b"
         )
      );

      await client.SearchAsync(
         new ActivitySearchRequest(
            CreateEntity(),
            new DateOnly(2026, 5, 31),
            AllowWebSearch: false
         ),
         CancellationToken.None
      );

      Assert.DoesNotContain("\"type\":\"web_search\"", handler.RequestBody);
   }

   [Fact]
   public async Task SearchCanUseConfiguredWebSearchToolType()
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var httpClient = new HttpClient(handler);
      var client = new OpenAiResponsesActivitySearchClient(
         httpClient,
         new OpenAiResponsesActivitySearchClientOptions(
            new Uri("http://127.0.0.1:1234/v1/"),
            "gpt-oss-20b",
            WebSearchToolType: "altra/web-search"
         )
      );

      await client.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      Assert.Contains("\"type\":\"altra/web-search\"", handler.RequestBody);
   }

   [Fact]
   public async Task SearchReturnsEmptyProposalsForNonJsonModelText()
   {
      var handler = new RecordingHandler(CreateTextResponseJson(
         "No reliable activity proposals were found."
      ));
      var httpClient = new HttpClient(handler);
      var client = new OpenAiResponsesActivitySearchClient(
         httpClient,
         new OpenAiResponsesActivitySearchClientOptions(
            new Uri("http://127.0.0.1:1234/v1/"),
            "gpt-oss-20b"
         )
      );

      var result = await client.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      Assert.Empty(result.Proposals);
      Assert.Equal(
         "No reliable activity proposals were found.",
         result.RawContent
      );
   }

   [Fact]
   public async Task SearchExceptionKeepsHttpStatusCode()
   {
      var handler = new RecordingHandler(
         """{"error":{"message":"Rate limit exceeded."}}""",
         HttpStatusCode.TooManyRequests
      );
      var httpClient = new HttpClient(handler);
      var client = new OpenAiResponsesActivitySearchClient(
         httpClient,
         new OpenAiResponsesActivitySearchClientOptions(
            new Uri("http://127.0.0.1:1234/v1/"),
            "gpt-oss-20b"
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

      Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
   }

   private static string CreateResponseJson(bool includeReasoning = false)
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
      var output = new List<object>();

      if (includeReasoning)
      {
         output.Add(new
         {
            type = "reasoning",
            content = new[]
            {
               new
               {
                  type = "reasoning_text",
                  text = "Need recent activity. Return JSON."
               }
            }
         });
      }

      output.Add(new
      {
         type = "message",
         content = new[]
         {
            new
            {
               type = "output_text",
               text = content
            }
         }
      });

      return JsonSerializer.Serialize(new
      {
         output
      });
   }

   private static string CreateTextResponseJson(string content)
   {
      var payload = new
      {
         output = new[]
         {
            new
            {
               content = new[]
               {
                  new { text = content }
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
