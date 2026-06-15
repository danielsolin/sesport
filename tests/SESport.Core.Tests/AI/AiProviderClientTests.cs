using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SESport.AI.Models;
using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public class AiProviderClientTests
{
   [Fact]
   public async Task LlamaServerGenerateAsyncUsesModelDrivenToolLoop()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaPageCallResponseJson(),
         CreateLlamaFinalResponseJson()
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/roster",
            "Sweden lineup info."
         )
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Article Title",
            "https://example.test/roster",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Article heading"],
            "Full article content.",
            true
         )
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         webSearchClient,
         webPageContentClient,
         new NoopLogger<LlamaServerClient>()
      );

      var result = await client.GenerateAsync(
         CreateProvider("llama-server"),
         CreateJob(
            "text",
            true,
            CreateToolsJson(),
            CreateToolsDescription()
         ),
         CreatePrompt(CreateParticipationSchemaJson()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      var expectedOutput =
         "{\"SwedishParticipation\":\"Yes\","
         + "\"SwedishParticipants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}";

      Assert.Equal(expectedOutput, result.OutputText);
      Assert.Contains("\"kind\":\"assistant\"", result.ToolTraceJson);
      Assert.Contains("\"kind\":\"tool\"", result.ToolTraceJson);
      Assert.Contains("Article Title", result.ToolTraceJson);
      Assert.DoesNotContain("\"sources\"", result.RawResponseJson);
      Assert.Equal(3, handler.RequestBodies.Count);
      Assert.Contains("\"role\":\"system\"", handler.RequestBodies[0]);
      Assert.Contains(
         CreateToolsDescription(),
         handler.RequestBodies[0]
      );
      Assert.Contains("\"role\":\"user\"", handler.RequestBodies[0]);
      Assert.Contains("\"tools\":[{\"type\":\"function\"",
         handler.RequestBodies[0]);
      Assert.Contains("\"tool_choice\":\"required\"",
         handler.RequestBodies[0]);
      Assert.Contains("\"name\":\"web_get_page\"",
         handler.RequestBodies[0]);
      Assert.Contains("\"role\":\"tool\"",
         handler.RequestBodies[1]);
      Assert.Contains("s1_1", handler.RequestBodies[1]);
      Assert.Contains("Tre Kronor roster", handler.RequestBodies[1]);
      Assert.Contains("\"role\":\"tool\"",
         handler.RequestBodies[2]);
      Assert.Contains("Article Title", handler.RequestBodies[2]);
      Assert.Contains("Full article content.", handler.RequestBodies[2]);
      Assert.DoesNotContain(
         "\"response_format\"",
         handler.RequestBodies[0]
      );
      Assert.Single(webSearchClient.Queries);
      Assert.Equal("Tre Kronor", webSearchClient.Queries[0].Query);
      Assert.Equal(10, webSearchClient.Queries[0].MaxResults);
      Assert.Single(webPageContentClient.Urls);
      Assert.Equal(
         "https://example.test/roster",
         webPageContentClient.Urls[0]
      );
   }

   [Fact]
   public async Task LlamaServerGenerateAsyncUsesDirectPageUrlToolCall()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallWithUrlResponseJson(),
         CreateLlamaFinalResponseJson(
            "https://example.test/direct-page"
         )
      );
      var webSearchClient = new RecordingWebSearchClient();
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Article Title",
            "https://example.test/direct-page",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Article heading"],
            "Full article content.",
            true
         )
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         webSearchClient,
         webPageContentClient,
         new NoopLogger<LlamaServerClient>()
      );

      var result = await client.GenerateAsync(
         CreateProvider("llama-server"),
         CreateJob(
            "text",
            true,
            CreateToolsJson(),
            CreateToolsDescription()
         ),
         CreatePrompt(CreateParticipationSchemaJson()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(
         "{\"SwedishParticipation\":\"Yes\","
         + "\"SwedishParticipants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/direct-page\"]}",
         result.OutputText
      );
      Assert.Empty(webSearchClient.Queries);
      Assert.Single(webPageContentClient.Urls);
      Assert.Equal(
         "https://example.test/direct-page",
         webPageContentClient.Urls[0]
      );
      Assert.Contains("\"url\":\"https://example.test/direct-page\"",
         result.ToolTraceJson);
   }

   [Fact]
   public async Task LlamaServerGenerateAsyncUsesSchemaForNonToolJobs()
   {
      var handler = new RecordingHandler(
         CreateLlamaFinalResponseJson()
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         new RecordingWebSearchClient(),
         new RecordingWebPageContentClient(null),
         new NoopLogger<LlamaServerClient>()
      );

      var result = await client.GenerateAsync(
         CreateProvider("llama-server"),
         CreateJob(
            "json_schema",
            requiresWebSearch: false,
            toolsJson: null,
            toolsDescription: null
         ),
         CreatePrompt(CreateParticipationSchemaJson()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(
         "{\"SwedishParticipation\":\"Yes\","
         + "\"SwedishParticipants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}",
         result.OutputText
      );
      Assert.Single(handler.RequestBodies);
      Assert.Contains(
         "\"response_format\":{\"type\":\"json_schema\"",
         handler.RequestBodies[0]
      );
      Assert.Contains(
         "\"schema\":{\"type\":\"object\"",
         handler.RequestBodies[0]
      );
   }

   [Fact]
   public async Task LlamaServerGenerateAsyncStopsAfterMaxToolRounds()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson()
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/roster",
            "Sweden lineup info."
         )
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Article Title",
            "https://example.test/roster",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Article heading"],
            "Full article content.",
            true
         )
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         webSearchClient,
         webPageContentClient,
         new NoopLogger<LlamaServerClient>()
      );

      var exception = await Assert.ThrowsAsync<
         AiProviderExecutionException
      >(
         () => client.GenerateAsync(
            CreateProvider("llama-server"),
            CreateJob(
               "text",
               true,
               CreateToolsJson(),
               CreateToolsDescription()
            ),
            CreatePrompt(
               CreateParticipationSchemaJson(),
               maxToolRounds: 1
            ),
            CreateRenderedPrompt(),
            "{}",
            CancellationToken.None
         )
      );

      Assert.Contains("Max tool rounds exceeded", exception.Message);
      Assert.Single(handler.RequestBodies);
   }

   [Fact]
   public async Task OpenRouterGenerateAsyncUsesChatCompletionsEnvelope()
   {
      var handler = new RecordingHandler(
         CreateChatResponseJson("{\"ok\":true}")
      );
      var client = new OpenRouterClient(new HttpClient(handler));

      var result = await client.GenerateAsync(
         CreateProvider("openrouter"),
         CreateJob("json_schema"),
         CreatePrompt(),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal("{\"ok\":true}", result.OutputText);
      Assert.Equal(new Uri("http://127.0.0.1:1234/v1/chat/completions"),
         handler.RequestUri);
      Assert.Contains("\"messages\":[{\"role\":\"system\"",
         handler.RequestBody);
      Assert.Contains("\"role\":\"user\"",
         handler.RequestBody);
      Assert.Contains("\"plugins\":[{\"id\":\"web\"}]",
         handler.RequestBody);
      Assert.Contains("\"response_format\":{\"type\":\"json_schema\"",
         handler.RequestBody);
   }

   [Fact]
   public async Task OpenRouterGenerateAsyncUsesSchemaEvenForJsonObjectMode()
   {
      var handler = new RecordingHandler(
         CreateChatResponseJson("{\"ok\":true}")
      );
      var client = new OpenRouterClient(new HttpClient(handler));

      await client.GenerateAsync(
         CreateProvider("openrouter"),
         CreateJob("json_object"),
         CreatePrompt(),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Contains("\"response_format\":{\"type\":\"json_schema\"",
         handler.RequestBody);
   }

   private static AiProviderDefinition CreateProvider(string kind)
   {
      return new AiProviderDefinition(
         "provider",
         "Provider",
         kind,
         "http://127.0.0.1:1234/v1/",
         "gpt-4o-2024-08-06",
         "key:secret-token",
         "{}",
         true
      );
   }

   private static AiJobDefinition CreateJob(
      string outputMode = "json_object",
      bool requiresWebSearch = true,
      string? toolsJson = null,
      string? toolsDescription = null
   )
   {
      return new AiJobDefinition(
         "job",
         "Job",
         null,
         "provider",
         outputMode,
         toolsJson,
         toolsDescription,
         requiresWebSearch,
         true,
         null
      );
   }

   private static string CreateToolsDescription()
   {
      return
         "When web_search returns promising results, inspect the most " +
         "relevant result pages with web_get_page before answering. " +
         "web_get_page can open either a search result id or a direct URL.";
   }

   private static string CreateToolsJson()
   {
      return JsonSerializer.Serialize(
         new object[]
         {
            new
            {
               type = "function",
               function = new
               {
                  name = "web_search",
                  description =
                     "Search the web for current or factual information.",
                  parameters = new
                  {
                     type = "object",
                     properties = new
                     {
                        query = new
                        {
                           type = "string"
                        },
                        limit = new
                        {
                           type = "integer",
                           minimum = 1,
                           maximum = 10
                        }
                     },
                     required = new[] { "query" },
                     additionalProperties = false
                  }
               }
            },
            new
            {
               type = "function",
               function = new
               {
                  name = "web_get_page",
                  description =
                     "Fetch the full page text for a search result id " +
                     "returned by web_search, or open a direct URL.",
                  parameters = new
                  {
                     type = "object",
                     properties = new
                     {
                        id = new
                        {
                           type = "string"
                        },
                        url = new
                        {
                           type = "string"
                        }
                     },
                     anyOf = new object[]
                     {
                        new
                        {
                           required = new[] { "id" }
                        },
                        new
                        {
                           required = new[] { "url" }
                        }
                     },
                     additionalProperties = false
                  }
               }
            }
         }
      );
   }

   private static AiPromptDefinition CreatePrompt(
      string? outputSchemaJson = """{"type":"object"}""",
      int? maxToolRounds = null
   )
   {
      return new AiPromptDefinition(
         Guid.Parse("11111111-1111-1111-1111-111111111111"),
         "job",
         1,
         "System",
         "User",
         outputSchemaJson,
         "{}",
         null,
         null,
         maxToolRounds,
         true
      );
   }

   private static AiRenderedPrompt CreateRenderedPrompt()
   {
      return new AiRenderedPrompt(
         "System",
         "User"
      );
   }

   private static string CreateReasoningResponseJson(string finalContent)
   {
      return JsonSerializer.Serialize(new
      {
         output = new object[]
         {
            new
            {
               type = "reasoning",
               content = new object[]
               {
                  new
                  {
                     type = "reasoning_text",
                     text = "Need JSON."
                  }
               }
            },
            new
            {
               type = "message",
               content = new object[]
               {
                  new
                  {
                     type = "output_text",
                     text = finalContent
                  }
               }
            }
         }
      });
   }

   private static string CreateChatResponseJson(
      string content,
      object[]? toolCalls = null,
      string? finishReason = null
   )
   {
      return JsonSerializer.Serialize(new
      {
         choices = new[]
         {
            new
            {
               message = new
               {
                  role = "assistant",
                  content
               },
               tool_calls = toolCalls,
               finish_reason = finishReason
            }
         },
         model = "openai/gpt-4o-2024-08-06"
      });
   }

   private static string CreateLlamaToolCallResponseJson()
   {
      return """
      {
        "choices": [
          {
            "message": {
              "role": "assistant",
              "content": "",
              "tool_calls": [
                {
                  "id": "call_1",
                  "type": "function",
                  "function": {
                    "name": "web_search",
                    "arguments": "{\"query\":\"Tre Kronor\",\"limit\":10}"
                  }
                }
              ]
            },
            "finish_reason": "tool_calls"
          }
        ],
        "model": "openai/gpt-4o-2024-08-06"
      }
      """;
   }

   private static string CreateLlamaPageCallResponseJson()
   {
      return """
      {
        "choices": [
          {
            "message": {
              "role": "assistant",
              "content": "",
              "tool_calls": [
                {
                  "id": "call_2",
                  "type": "function",
                  "function": {
                    "name": "web_get_page",
                    "arguments": "{\"id\":\"s1_1\"}"
                  }
                }
              ]
            },
            "finish_reason": "tool_calls"
          }
        ],
        "model": "openai/gpt-4o-2024-08-06"
      }
      """;
   }

   private static string CreateLlamaToolCallWithUrlResponseJson()
   {
      return """
      {
        "choices": [
          {
            "message": {
              "role": "assistant",
              "content": "",
              "tool_calls": [
                {
                  "id": "call_1",
                  "type": "function",
                  "function": {
                    "name": "web_get_page",
                    "arguments":
                      "{\"url\":\"https://example.test/direct-page\"}"
                  }
                }
              ]
            },
            "finish_reason": "tool_calls"
          }
        ],
        "model": "openai/gpt-4o-2024-08-06"
      }
      """;
   }

   private static string CreateLlamaPageCallWithUrlResponseJson()
   {
      return """
      {
        "choices": [
          {
            "message": {
              "role": "assistant",
              "content": "",
              "tool_calls": [
                {
                  "id": "call_2",
                  "type": "function",
                  "function": {
                    "name": "web_get_page",
                    "arguments":
                      "{\"url\":\"https://example.test/direct-page\"}"
                  }
                }
              ]
            },
            "finish_reason": "tool_calls"
          }
        ],
        "model": "openai/gpt-4o-2024-08-06"
      }
      """;
   }

   private static string CreateLlamaFinalResponseJson()
   {
      return CreateLlamaFinalResponseJson("https://example.test/roster");
   }

   private static string CreateLlamaFinalResponseJson(string sourceUrl)
   {
      var content =
         "{\"SwedishParticipation\":\"Yes\","
         + "\"SwedishParticipants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"" + sourceUrl + "\"]}";

      return JsonSerializer.Serialize(
         new
         {
            choices = new[]
            {
               new
               {
                  message = new
                  {
                     role = "assistant",
                     content
                  },
                  finish_reason = "stop"
               }
            },
            model = "openai/gpt-4o-2024-08-06"
         }
      );
   }

   private static string CreateParticipationSchemaJson()
   {
      return """
      {
        "type": "object",
        "properties": {
          "SwedishParticipation": {
            "type": "string"
          },
          "SwedishParticipants": {
            "type": "array",
            "items": {
              "type": "string"
            }
          },
          "Sources": {
            "type": "array",
            "items": {
              "type": "string",
              "format": "uri"
            }
          }
        },
        "required": [
          "SwedishParticipation",
          "SwedishParticipants",
          "Sources"
        ],
        "additionalProperties": false
      }
      """;
   }

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly Queue<string> responseJson;

      public RecordingHandler(params string[] responseJson)
      {
         this.responseJson = new Queue<string>(responseJson);
      }

      public Uri? RequestUri { get; private set; }

      public List<string> RequestBodies { get; } = [];

      public string RequestBody =>
         RequestBodies.Count == 0 ? "" : RequestBodies[^1];

      protected override async Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestUri = request.RequestUri;
         var requestBody = request.Content is null
            ? ""
            : await request.Content.ReadAsStringAsync(cancellationToken);
         RequestBodies.Add(requestBody);

         var response = responseJson.Count == 0
            ? "{}"
            : responseJson.Dequeue();

         return new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = JsonContent.Create(
               JsonSerializer.Deserialize<JsonElement>(response)
            )
         };
      }
   }

   private sealed class RecordingWebSearchClient : IWebSearchClient
   {
      private readonly IReadOnlyList<WebSearchResult> results;

      public RecordingWebSearchClient(
         params WebSearchResult[] results
      )
      {
         this.results = results;
      }

      public List<(string Query, int MaxResults)> Queries { get; } = [];

      public Task<IReadOnlyList<WebSearchResult>> SearchAsync(
         string query,
         int maxResults,
         CancellationToken cancellationToken
      )
      {
         Queries.Add((query, maxResults));
         return Task.FromResult(results);
      }
   }

   private sealed class RecordingWebPageContentClient
      : IWebPageContentClient
   {
      private readonly WebPageContent? content;

      public RecordingWebPageContentClient(WebPageContent? content)
      {
         this.content = content;
      }

      public List<string> Urls { get; } = [];

      public Task<WebPageContent?> FetchAsync(
         string url,
         CancellationToken cancellationToken
      )
      {
         Urls.Add(url);
         return Task.FromResult(content);
      }
   }

   private sealed class NoopLogger<T> : ILogger<T>
   {
      IDisposable ILogger.BeginScope<TState>(TState state)
      {
         return EmptyDisposable.Instance;
      }

      public bool IsEnabled(LogLevel logLevel)
      {
         return false;
      }

      void ILogger.Log<TState>(
         LogLevel logLevel,
         EventId eventId,
         TState state,
         Exception? exception,
         Func<TState, Exception?, string> formatter
      )
      {
      }

      private sealed class EmptyDisposable : IDisposable
      {
         public static readonly EmptyDisposable Instance = new();

         public void Dispose()
         {
         }
      }
   }
}
