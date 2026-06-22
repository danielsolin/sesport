using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using SESport.AI.Interfaces;
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
            CreateToolsJson()
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
      Assert.Contains("\"kind\":\"budget\"", result.ToolTraceJson);
      Assert.Contains("\"kind\":\"assistant\"", result.ToolTraceJson);
      Assert.Contains("\"kind\":\"tool\"", result.ToolTraceJson);
      Assert.Contains("Article Title", result.ToolTraceJson);
      Assert.DoesNotContain("\"sources\"", result.RawResponseJson);
      Assert.Equal(3, handler.RequestBodies.Count);
      Assert.Contains("\"role\":\"system\"", handler.RequestBodies[0]);
      Assert.Contains("\"role\":\"user\"", handler.RequestBodies[0]);
      Assert.Contains("\"tools\":[{\"type\":\"function\"",
         handler.RequestBodies[0]);
      Assert.Contains("\"tool_choice\":\"required\"",
         handler.RequestBodies[0]);
      Assert.Contains("\"tool_choice\":\"auto\"",
         handler.RequestBodies[1]);
      Assert.Contains("\"tool_choice\":\"auto\"",
         handler.RequestBodies[2]);
      Assert.Contains(
         $"\"name\":\"{WebToolNames.GetPage}\"",
         handler.RequestBodies[0]);
      Assert.Contains("\"role\":\"tool\"",
         handler.RequestBodies[1]);
      Assert.Contains("https://example.test/roster",
         handler.RequestBodies[1]);
      Assert.Contains("Tre Kronor roster", handler.RequestBodies[1]);
      Assert.Contains("\"role\":\"tool\"",
         handler.RequestBodies[2]);
      Assert.Contains("Article Title", handler.RequestBodies[2]);
      Assert.Contains("Full article content.", handler.RequestBodies[2]);
      Assert.Equal(handler.RequestBodies[2], result.RawRequestJson);
      Assert.Contains(
         $"\"name\":\"{WebToolNames.FindInPage}\"",
         handler.RequestBodies[0]);
      Assert.DoesNotContain(
         "\"response_format\"",
         handler.RequestBodies[0]
      );
      Assert.DoesNotContain(
         "\"tool_choice\":\"required\"",
         handler.RequestBodies[1]
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
   public async Task
      LlamaServerGenerateAsyncRetriesWhenStructuredOutputFormatFails()
   {
      var handler = new RecordingHandler(
         new RecordingHandler.ResponseSpec(
            HttpStatusCode.InternalServerError,
            """
            {
               "error": {
                  "code": 500,
                  "message": "The model produced output that does not match the expected peg-native format",
                  "type": "server_error"
               }
            }
            """
         ),
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
         CreateJob("text", requiresWebSearch: false, null),
         CreatePrompt(CreateParticipationSchemaJson()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestBodies.Count);
      Assert.Contains(
         "Return only the raw JSON object",
         handler.RequestBodies[1]
      );
      Assert.Equal(
         "{\"SwedishParticipation\":\"Yes\","
         + "\"SwedishParticipants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}",
         result.OutputText
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncOmitsAssistantContentForToolCalls()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJsonWithContent(
            "<|channel|>commentary<|message|>noise"
         ),
         CreateLlamaFinalResponseJson()
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         new RecordingWebSearchClient(),
         new RecordingWebPageContentClient(null),
         new NoopLogger<LlamaServerClient>()
      );

      await client.GenerateAsync(
         CreateProvider("llama-server"),
         CreateJob("text", requiresWebSearch: true, CreateToolsJson()),
         CreatePrompt(CreateParticipationSchemaJson()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestBodies.Count);
      Assert.DoesNotContain(
         "<|channel|>commentary<|message|>noise",
         handler.RequestBodies[1]
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncReportsWhenPageFetchFails()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaPageCallResponseJson(),
         CreateLlamaFinalResponseJson()
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         new RecordingWebSearchClient(
            new WebSearchResult(
               "Tre Kronor roster",
               "https://example.test/roster",
               "Sweden lineup info."
            )
         ),
         new RecordingWebPageContentClient(null),
         new NoopLogger<LlamaServerClient>()
      );

      var result = await client.GenerateAsync(
         CreateProvider("llama-server"),
         CreateJob(
            "text",
            true,
            CreateToolsJson()
         ),
         CreatePrompt(CreateParticipationSchemaJson()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Contains(
         "Unable to fetch page content from",
         result.ToolTraceJson
      );
      Assert.Contains("example.test", result.ToolTraceJson);
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncKeepsLatestCompletedRoundDuringTrim()
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
      var hugePageText = string.Join(
         "",
         Enumerable.Repeat("KEEP-ME-ROUND-2-", 2000)
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Huge Article",
            "https://example.test/roster",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Article heading"],
            hugePageText,
            true,
            hugePageText
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
            CreateToolsJson()
         ),
         CreatePrompt(CreateParticipationSchemaJson()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(3, handler.RequestBodies.Count);
      Assert.Contains("Conversation history summary:",
         handler.RequestBodies[2]);
      Assert.Equal(handler.RequestBodies[2], result.RawRequestJson);
   }

   [Fact]
   public async Task LlamaServerGenerateAsyncRetriesLoadingModel503()
   {
      var handler = new RecordingHandler(
         new RecordingHandler.ResponseSpec(
            HttpStatusCode.ServiceUnavailable,
            CreateLoadingModelResponseJson()
         ),
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
            toolsJson: null
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
      Assert.Equal(2, handler.RequestBodies.Count);
   }

   [Fact]
   public async Task LlamaServerGenerateAsyncRetriesTransportSendFailure()
   {
      var handler = new RecordingHandler(
         new HttpRequestException(
            "An error occurred while sending the request."
         ),
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
            toolsJson: null
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
      Assert.Equal(2, handler.RequestBodies.Count);
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncReusesRepeatedPageFindCallsWithoutAnnotation()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaFindPageCallResponseJson(),
         CreateLlamaFindPageCallWithUrlResponseJson(),
         CreateLlamaFinalResponseJson()
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/direct-page",
            "Sweden lineup info."
         )
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Article Title",
            "https://example.test/direct-page",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Article heading"],
            "No relevant mention here.",
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
            "json_schema",
            requiresWebSearch: false,
            toolsJson: null
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
      Assert.Equal(2, webPageContentClient.Urls.Count);
      Assert.Equal(4, handler.RequestBodies.Count);
      Assert.DoesNotContain("already made in round",
         handler.RequestBodies[3]);
      Assert.DoesNotContain("Do not repeat this check.",
         handler.RequestBodies[3]);
      Assert.DoesNotContain("Reuse the previous result",
         handler.RequestBodies[3]);
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
            CreateToolsJson()
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
   public async Task LlamaServerGenerateAsyncCachesPageFetchesForFindTools()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaPageCallWithFindExtraTokenResponseJson(),
         CreateLlamaFindPageCallExtraTokenResponseJson(),
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
            "Short article.",
            true,
            "Short article. ExtraToken appears here."
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
            CreateToolsJson()
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
      Assert.Equal(4, handler.RequestBodies.Count);
      Assert.Contains(
         $"\"name\":\"{WebToolNames.GetPage}\"",
         result.ToolTraceJson);
      Assert.Contains("\"find\":\"ExtraToken\"",
         result.ToolTraceJson);
      Assert.Contains(
         $"\"name\":\"{WebToolNames.FindInPage}\"",
         result.ToolTraceJson);
      Assert.Single(webPageContentClient.Urls);
      Assert.Contains(
         handler.RequestBodies,
         body => body.Contains("ExtraToken", StringComparison.Ordinal)
      );
   }

   [Fact]
   public void FindPageMatchesIgnoresTitleAndHeadings()
   {
      var matches = InvokeFindPageMatches(
         new WebPageContent(
            "Sweden Title",
            "https://example.test/roster",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Sweden Heading"],
            "No relevant mention here.",
            true,
            "No relevant mention here."
         ),
         "Sweden"
      );

      Assert.Empty(matches);
   }

   [Fact]
   public void FindPageMatchesLimitsTextSnippetsToTwenty()
   {
      var body = string.Join(
         " ",
         Enumerable.Range(0, 25).Select(index =>
            $"chunk-{index}-before Sweden chunk-{index}-after")
      );
      var matches = InvokeFindPageMatches(
         new WebPageContent(
            "Article Title",
            "https://example.test/roster",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            [],
            body,
            true,
            body
         ),
         "Sweden"
      );

      Assert.Equal(20, matches.Count);
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
            toolsJson: null
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
   public async Task LlamaServerGenerateAsyncFallsBackAfterMaxToolRounds()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
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
            CreateToolsJson()
         ),
         CreatePrompt(
            CreateParticipationSchemaJson(),
            maxToolRounds: 1
         ),
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
      Assert.Contains("\"kind\":\"budget\"", result.ToolTraceJson);
      Assert.Equal(2, handler.RequestBodies.Count);
      Assert.Contains(
         "Tool calls remaining: 1 of 1.",
         handler.RequestBodies[0]
      );
      Assert.Contains(
         "Tool calls remaining: 0 of 1.",
         handler.RequestBodies[1]
      );
      Assert.Contains("\"tool_choice\":\"required\"",
         handler.RequestBodies[0]);
      Assert.Contains("\"tools\":[",
         handler.RequestBodies[0]);
      Assert.DoesNotContain("\"tools\":[",
         handler.RequestBodies[1]);
      Assert.DoesNotContain("\"tool_choice\"",
         handler.RequestBodies[1]);
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncRetriesWhenFinalStructuredOutputIsInvalid()
   {
      var handler = new RecordingHandler(
         CreateLlamaInvalidFinalResponseJson(),
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
         CreateJob("json_schema", requiresWebSearch: false, null),
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
      Assert.Equal(2, handler.RequestBodies.Count);
      Assert.Contains(
         "Return only the raw JSON object required by the schema.",
         handler.RequestBodies[1]
      );
      Assert.NotNull(result.ToolTraceJson);
      Assert.Equal(
         3,
         CountOccurrences(result.ToolTraceJson!, "\"kind\":\"assistant\"")
      );
      Assert.Equal(
         1,
         CountOccurrences(
            result.ToolTraceJson!,
            "\"validation_status\":\"rejected\""
         )
      );
      Assert.Equal(
         1,
         CountOccurrences(
            result.ToolTraceJson!,
            "\"validation_status\":\"accepted\""
         )
      );
      Assert.Contains(
         "Return only the raw JSON object required by the schema.",
         result.ToolTraceJson
      );
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
      string? toolsJson = null
   )
   {
      return new AiJobDefinition(
         "job",
         "Job",
         null,
         "provider",
         outputMode,
         toolsJson,
         requiresWebSearch,
         true,
         null
      );
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
                  name = WebToolNames.Search,
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
                  name = WebToolNames.GetPage,
                  description =
                     "Fetch the full page text from a URL.",
                  parameters = new
                  {
                     type = "object",
                     properties = new
                     {
                        url = new
                        {
                           type = "string"
                        }
                     },
                     required = new[] { "url" },
                     additionalProperties = false
                  }
               }
            },
            new
            {
               type = "function",
               function = new
               {
                  name = WebToolNames.FindInPage,
                  description =
                     "Find matching text in a fetched page or direct URL.",
                  parameters = new
                  {
                     type = "object",
                     properties = new
                     {
                        url = new
                        {
                           type = "string"
                        },
                        find = new
                        {
                           type = "string"
                        }
                     },
                     required = new[] { "find", "url" },
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
      return $$"""
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
                    "name": "{{WebToolNames.Search}}",
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

   private static string CreateLlamaToolCallResponseJsonWithContent(
      string content
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
                  @role = "assistant",
                  content,
                  tool_calls = new[]
                  {
                     new
                     {
                        id = "call_1",
                        type = "function",
                        function = new
                        {
                           name = WebToolNames.Search,
                           arguments =
                              "{\"query\":\"Tre Kronor\",\"limit\":10}"
                        }
                     }
                  }
               },
               finish_reason = "tool_calls"
            }
         },
         model = "openai/gpt-4o-2024-08-06"
      });
   }

   private static string CreateLlamaPageCallResponseJson()
   {
      return $$"""
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
                    "name": "{{WebToolNames.GetPage}}",
                    "arguments":
                      "{\"url\":\"https://example.test/roster\"}"
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
      return $$"""
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
                    "name": "{{WebToolNames.GetPage}}",
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
      return $$"""
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
                    "name": "{{WebToolNames.GetPage}}",
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

   private static string CreateLlamaFindPageCallWithUrlResponseJson()
   {
      return JsonSerializer.Serialize(new
      {
         choices = new[]
         {
            new
            {
               message = new
               {
                  @role = "assistant",
                  content = "",
                  tool_calls = new[]
                  {
                     new
                     {
                        id = "call_2",
                        type = "function",
                        function = new
                        {
                           name = WebToolNames.FindInPage,
                           arguments =
                              "{\"url\":\"https://example.test/direct-page\"," +
                              "\"find\":\"Sweden\"}"
                        }
                     }
                  }
               },
               finish_reason = "tool_calls"
            }
         },
         model = "openai/gpt-4o-2024-08-06"
      });
   }

   private static string CreateLlamaPageCallWithFindResponseJson()
   {
      return $$"""
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
                    "name": "{{WebToolNames.GetPage}}",
                    "arguments": "{\"url\":\"https://example.test/roster\",\"find\":\"Sweden\"}"
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

   private static string CreateLlamaPageCallWithFindExtraTokenResponseJson()
   {
      return JsonSerializer.Serialize(new
      {
         choices = new[]
         {
            new
            {
               message = new
               {
                  @role = "assistant",
                  content = "",
                  tool_calls = new[]
                  {
                     new
                     {
                        id = "call_2",
                        type = "function",
                        function = new
                        {
                           name = WebToolNames.GetPage,
                           arguments =
                              "{\"url\":\"https://example.test/roster\"," +
                              "\"find\":\"ExtraToken\"}"
                        }
                     }
                  }
               },
               finish_reason = "tool_calls"
            }
         },
         model = "openai/gpt-4o-2024-08-06"
      });
   }

   private static string CreateLlamaFindPageCallResponseJson()
   {
      return $$"""
      {
        "choices": [
          {
            "message": {
              "role": "assistant",
              "content": "",
              "tool_calls": [
                {
                  "id": "call_3",
                  "type": "function",
                  "function": {
                    "name": "{{WebToolNames.FindInPage}}",
                    "arguments": "{\"url\":\"https://example.test/roster\",\"find\":\"Sweden\"}"
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

   private static string CreateLlamaFindPageCallExtraTokenResponseJson()
   {
      return JsonSerializer.Serialize(new
      {
         choices = new[]
         {
            new
            {
               message = new
               {
                  @role = "assistant",
                  content = "",
                  tool_calls = new[]
                  {
                     new
                     {
                        id = "call_3",
                        type = "function",
                        function = new
                        {
                           name = WebToolNames.FindInPage,
                           arguments =
                              "{\"url\":\"https://example.test/roster\"," +
                              "\"find\":\"ExtraToken\"}"
                        }
                     }
                  }
               },
               finish_reason = "tool_calls"
            }
         },
         model = "openai/gpt-4o-2024-08-06"
      });
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

   private static string CreateLlamaInvalidFinalResponseJson()
   {
      var content =
         "{\"SwedishParticipation\":\"Yes\","
         + "\"SwedishParticipants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]";

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

   private static string CreateLoadingModelResponseJson()
   {
      return """
      {
        "error": {
          "message": "Loading model",
          "type": "unavailable_error",
          "code": 503
        }
      }
      """;
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

   private static int CountOccurrences(string value, string pattern)
   {
      var count = 0;
      var index = 0;

      while(true)
      {
         index = value.IndexOf(
            pattern,
            index,
            StringComparison.Ordinal
         );

         if(index < 0)
         {
            return count;
         }

         count++;
         index += pattern.Length;
      }
   }

   private static System.Collections.ICollection InvokeFindPageMatches(
      WebPageContent pageContent,
      string find
   )
   {
      var method = typeof(LlamaServerClient).GetMethod(
         "FindPageMatches",
         System.Reflection.BindingFlags.NonPublic |
         System.Reflection.BindingFlags.Static
      );

      if(method is null)
      {
         throw new InvalidOperationException(
            "Unable to find FindPageMatches via reflection."
         );
      }

      var matches = method.Invoke(null, [pageContent, find]);

      if(matches is not System.Collections.ICollection collection)
      {
         throw new InvalidOperationException(
            "FindPageMatches did not return a collection."
         );
      }

      return collection;
   }

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly Queue<object> responses;

      public RecordingHandler(params object[] responses)
      {
         this.responses = new Queue<object>(responses);
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

         var response = responses.Count == 0
            ? "{}"
            : responses.Dequeue();

         return response switch
         {
            string json => CreateResponseMessage(
               HttpStatusCode.OK,
               json
            ),
            ResponseSpec spec => CreateResponseMessage(
               spec.StatusCode,
               spec.Body
            ),
            Exception exception => throw exception,
            _ => throw new InvalidOperationException(
               $"Unsupported response type '{response.GetType()}'."
            )
         };
      }

      private static HttpResponseMessage CreateResponseMessage(
         HttpStatusCode statusCode,
         string body
      )
      {
         return new HttpResponseMessage(statusCode)
         {
            Content = JsonContent.Create(
               JsonSerializer.Deserialize<JsonElement>(body)
            )
         };
      }

      public sealed record ResponseSpec(
         HttpStatusCode StatusCode,
         string Body
      );
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

      public Task<WebSearchResponse> SearchAsync(
         string query,
         int maxResults,
         CancellationToken cancellationToken
      )
      {
         Queries.Add((query, maxResults));
         return Task.FromResult(new WebSearchResponse(results));
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
