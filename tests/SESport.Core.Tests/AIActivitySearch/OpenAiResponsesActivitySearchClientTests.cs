using System.Net;
using System.Text;
using System.Text.Json;
using SESport.AI.ActivitySearch;
using SESport.Core.Domain;

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
      Assert.Contains(
         $"\"type\":\"{WebToolNames.Search}\"",
         handler.RequestBody
      );
      Assert.Contains("Tre Kronor vs Finland", result.RawContent);
      Assert.Equal("gpt-oss-20b", result.Producer);
      Assert.Contains("Tre Kronor", result.Prompt);
      Assert.Contains("Tre Kronor", handler.RequestBody);
      Assert.Single(result.Proposals);
   }

   [Fact]
   public async Task SearchPrefixesActualOpenRouterModelAsProducer()
   {
      var handler = new RecordingHandler(
         CreateResponseJson(
            model: "openrouter/free",
            openRouterSelectedModel: "openai/gpt-oss-20b"
         )
      );
      var httpClient = new HttpClient(handler);
      var client = new OpenAiResponsesActivitySearchClient(
         httpClient,
         new OpenAiResponsesActivitySearchClientOptions(
            new Uri("https://openrouter.ai/api/v1/"),
            "openrouter/free"
         )
      );

      var result = await client.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      Assert.Equal("enabled", handler.OpenRouterMetadataHeader);
      Assert.Equal("openrouter/openai/gpt-oss-20b", result.Producer);
   }

   [Fact]
   public async Task SearchCanUseConfiguredWebSearchToolType()
   {
      const string customWebSearchToolType = "custom-web-search";

      var handler = new RecordingHandler(CreateResponseJson());
      var httpClient = new HttpClient(handler);
      var client = new OpenAiResponsesActivitySearchClient(
         httpClient,
         new OpenAiResponsesActivitySearchClientOptions(
            new Uri("http://127.0.0.1:1234/v1/"),
            "gpt-oss-20b",
            WebSearchToolType: customWebSearchToolType
         )
      );

      await client.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      Assert.Contains(
         $"\"type\":\"{customWebSearchToolType}\"",
         handler.RequestBody
      );
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

   [Fact]
   public async Task SearchRetriesMalformedSuccessfulResponseEnvelope()
   {
      var handler = new RecordingHandler(
         ["\n\n", CreateResponseJson()]
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

      Assert.Equal(2, handler.RequestCount);
      Assert.Single(result.Proposals);
   }

   private static string CreateResponseJson(
      bool includeReasoning = false,
      string? model = null,
      string? openRouterSelectedModel = null
   )
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

      object? openRouterMetadata = openRouterSelectedModel is null
         ? null
         : new
         {
            requested = model,
            strategy = "free",
            endpoints = new
            {
               available = new[]
               {
                  new
                  {
                     provider = "OpenAI",
                     model = openRouterSelectedModel,
                     selected = true
                  }
               }
            }
         };

      return JsonSerializer.Serialize(new
      {
         model,
         openrouter_metadata = openRouterMetadata,
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

      public string? OpenRouterMetadataHeader { get; private set; }

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
         OpenRouterMetadataHeader = request.Headers.TryGetValues(
            "X-OpenRouter-Experimental-Metadata",
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
