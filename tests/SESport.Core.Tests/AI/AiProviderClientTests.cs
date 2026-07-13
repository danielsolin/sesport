using Microsoft.Extensions.Logging;
using SESport.AI.Clients;
using SESport.AI.Interfaces;
using SESport.AI.Llama;
using SESport.Core.AI;
using SESport.Core.Domain;
using SESport.AI.WebPages;
using SESport.AI.WebSearch;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.Core.Tests.AI;

public class AiProviderClientTests
{
   [Fact]
   public async Task
      LlamaServerGenerateAsyncAcceptsEarlyReportWithParticipant()
   {
      var sourceUrl = "https://example.test/news/line-up";
      var participantName = "Armand Duplantis";
      var acceptedOutput =
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[{\"Name\":\"" + participantName + "\","
         + "\"Sources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"ParticipantMention\"}]}],"
         + "\"CheckedSources\":[]}";
      var emptyOutput =
         "{\"Participation\":\"Unknown\","
         + "\"Participants\":[],"
         + "\"CheckedSources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"EventInfoOnly\"}]}";
      var handler = new RecordingHandler(
         CreateLlamaSubmitReportResponseJson(emptyOutput),
         CreateLlamaToolCallResponseJson("first research query"),
         CreateLlamaSubmitReportResponseJson(emptyOutput),
         CreateLlamaToolCallResponseJson("second research query"),
         CreateLlamaSubmitReportResponseJson(emptyOutput),
         CreateLlamaPageCallResponseJson(sourceUrl),
         CreateLlamaSubmitReportResponseJson(acceptedOutput)
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Line-up announcement",
            sourceUrl,
            null,
            [],
            $"{participantName} is officially confirmed to compete. " +
               $"The {PrimaryCountry.LanguageName} athlete will start.",
            true
         )
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         new RecordingWebSearchClient(),
         webPageContentClient,
         new NoopLogger<LlamaServerClient>()
      );

      var result = await client.GenerateAsync(
         CreateProvider("llama-server"),
         CreateJob(
            "json_schema",
            requiresWebSearch: true,
            toolsJson: CreateToolsJson(),
            conditionalToolsJson: CreateConditionalToolsJson(),
            jobId: AiJobIds.DecidePrimaryCountryParticipation
         ),
         CreatePrompt(
            CreateParticipationSchemaJsonWithEvidenceType(),
            maxToolRounds: 4
         ),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(acceptedOutput, result.OutputText);
      Assert.Equal(3, result.ToolRoundCount);
      Assert.Equal(7, handler.RequestBodies.Count);
      Assert.Contains(
         $"\"name\":\"{LlamaReportSubmission.ToolName}\"",
         handler.RequestBodies[0]);
      var initialRequest = JsonNode.Parse(handler.RequestBodies[0]);
      var reportTool = initialRequest?["tools"]?
         .AsArray()
         .OfType<JsonObject>()
         .Single(tool => string.Equals(
            tool["function"]?["name"]?.GetValue<string>(),
            LlamaReportSubmission.ToolName,
            StringComparison.Ordinal
         ));
      Assert.Equal(
         1,
         reportTool?["function"]?["parameters"]?["properties"]?
            ["Participants"]?["minItems"]?.GetValue<int>()
      );
      Assert.DoesNotContain(
         $"\"name\":\"{LlamaReportSubmission.ToolName}\"",
         handler.RequestBodies[1]);
      Assert.Contains("requires at least one supported participant",
         result.ToolTraceJson);
      Assert.Contains(
         "\"conditional_tools\":[{\"name\":\"submit_report\"",
         result.ToolTraceJson
      );
      Assert.Contains(
         "\"kind\":\"submission\"",
         result.ToolTraceJson
      );
      Assert.Contains(
         "\"tool_call_id\"",
         result.ToolTraceJson
      );
      Assert.DoesNotContain(
         "\"tool\":{\"type\":\"function\"",
         result.ToolTraceJson
      );
   }

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
            $"{PrimaryCountry.CountryName} lineup info."
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
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
      Assert.Contains("\"tool_choice\":\"required\"",
         handler.RequestBodies[1]);
      Assert.Contains("\"tool_choice\":\"required\"",
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
      Assert.Contains("\"search_engine\":\"google\"", result.ToolTraceJson);
      Assert.Single(webSearchClient.Queries);
      Assert.Equal("Tre Kronor", webSearchClient.Queries[0].Query);
      Assert.Equal(10, webSearchClient.Queries[0].MaxResults);
      Assert.Equal(0, webSearchClient.SearchAttempts[0]);
      Assert.Single(webPageContentClient.Urls);
      Assert.Equal(
         "https://example.test/roster",
         webPageContentClient.Urls[0]
      );
   }

   [Fact]
   public async Task LlamaServerGenerateAsyncStoresReasoningInToolTrace()
   {
      var handler = new RecordingHandler(
         CreateLlamaFinalResponseJsonWithReasoning("Need JSON.")
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

      Assert.Contains(
         "\"reasoning_content\":\"Need JSON.\"",
         result.ToolTraceJson
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncRetriesWhenStructuredOutputFormatFails()
   {
      var handler = new RecordingHandler(
         new RecordingHandler.ResponseSpec(
            HttpStatusCode.InternalServerError,
            "{"
            + "\"error\":{"
            + "\"code\":500,"
            + "\"message\":\"The model produced output that does not " +
            "match the expected peg-native format\","
            + "\"type\":\"server_error\""
            + "}"
            + "}"
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
         "Return only one raw object literal",
         handler.RequestBodies[1]
      );
      Assert.DoesNotContain(
         "\"response_format\"",
         handler.RequestBodies[1]
      );
      Assert.Contains("\"kind\":\"repair_prompt\"", result.ToolTraceJson);
      Assert.Equal(
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}",
         result.OutputText
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncRetriesStructuredOutputRepairMoreThanOnce()
   {
      var handler = new RecordingHandler(
         new RecordingHandler.ResponseSpec(
            HttpStatusCode.InternalServerError,
            "{"
            + "\"error\":{"
            + "\"code\":500,"
            + "\"message\":\"The model produced output that does not " +
            "match the expected peg-native format\","
            + "\"type\":\"server_error\""
            + "}"
            + "}"
         ),
         new RecordingHandler.ResponseSpec(
            HttpStatusCode.InternalServerError,
            "{"
            + "\"error\":{"
            + "\"code\":500,"
            + "\"message\":\"The model produced output that does not " +
            "match the expected peg-native format\","
            + "\"type\":\"server_error\""
            + "}"
            + "}"
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

      Assert.Equal(3, handler.RequestBodies.Count);
      Assert.Contains(
         "Return only one raw object literal",
         handler.RequestBodies[1]
      );
      Assert.DoesNotContain(
         "\"response_format\"",
         handler.RequestBodies[1]
      );
      Assert.Contains(
         "Return only one raw object literal",
         handler.RequestBodies[2]
      );
      Assert.DoesNotContain(
         "\"response_format\"",
         handler.RequestBodies[2]
      );
      Assert.Equal(
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}",
         result.OutputText
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncContinuesToolsAfterToolFormatFailure()
   {
      var handler = new RecordingHandler(
         CreatePegNativeFormatError(),
         CreateLlamaToolCallResponseJson("Tre Kronor roster"),
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
         CreateJob("text", requiresWebSearch: true, CreateToolsJson()),
         CreatePrompt(CreateParticipationSchemaJson()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(3, handler.RequestBodies.Count);
      Assert.Contains("\"tools\":[", handler.RequestBodies[0]);
      Assert.Contains("\"tool_choice\":\"required\"",
         handler.RequestBodies[0]);
      Assert.Contains("\"tools\":[", handler.RequestBodies[1]);
      Assert.Contains("\"tool_choice\":\"required\"",
         handler.RequestBodies[1]);
      Assert.Contains(
         "previous tool-call attempt could not be parsed",
         handler.RequestBodies[1]
      );
      Assert.DoesNotContain(
         handler.RequestBodies,
         body => body.Contains(
            "The previous response was rejected",
            StringComparison.Ordinal
         )
      );
      Assert.Contains("tool_format_fallback", result.ToolTraceJson);
      Assert.Contains("Retrying with tools", result.ToolTraceJson);
      Assert.DoesNotContain("repair_prompt", result.ToolTraceJson);
      Assert.Equal(
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
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
               $"{PrimaryCountry.CountryName} lineup info."
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
            $"{PrimaryCountry.CountryName} lineup info."
         )
      );
      var hugePageText = string.Join(
         "",
         Enumerable.Repeat("KEEP-ME-ROUND-2-", 18000)
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
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
            $"{PrimaryCountry.CountryName} lineup info."
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
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
   public async Task
      LlamaServerGenerateAsyncRotatesSearchEnginesBeforeBlockingRepeat()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaToolCallResponseJson(),
         CreateLlamaToolCallResponseJson(),
         CreateLlamaToolCallResponseJson(),
         CreateLlamaFinalResponseJson()
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/roster",
            $"{PrimaryCountry.CountryName} lineup info."
         )
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         webSearchClient,
         new RecordingWebPageContentClient(null),
         new NoopLogger<LlamaServerClient>()
      );

      await client.GenerateAsync(
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

      Assert.Equal(3, webSearchClient.Queries.Count);
      Assert.Equal(3, webSearchClient.SearchAttempts.Count);
      Assert.Equal(0, webSearchClient.SearchAttempts[0]);
      Assert.Equal(1, webSearchClient.SearchAttempts[1]);
      Assert.Equal(2, webSearchClient.SearchAttempts[2]);
      Assert.All(
         webSearchClient.Queries,
         query => Assert.Equal("Tre Kronor", query.Query)
      );
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
            $"{PrimaryCountry.CountryName} lineup info."
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
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
      var matches = LlamaPageToolFormatter.FindPageMatches(
         new WebPageContent(
            $"{PrimaryCountry.CountryName} Title",
            "https://example.test/roster",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            [$"{PrimaryCountry.CountryName} Heading"],
            "No relevant mention here.",
            true,
            "No relevant mention here."
         ),
         PrimaryCountry.CountryName
      );

      Assert.Empty(matches);
   }

   [Fact]
   public void FindPageMatchesLimitsTextSnippetsToTwenty()
   {
      var body = string.Join(
         " ",
         Enumerable.Range(0, 25).Select(index =>
            $"chunk-{index}-before " +
            $"{new string('x', 70)} id-{index} " +
            $"{PrimaryCountry.CountryName} " +
            $"{new string('y', 70)} " +
            $"chunk-{index}-after")
      );
      var matches = LlamaPageToolFormatter.FindPageMatches(
         new WebPageContent(
            "Article Title",
            "https://example.test/roster",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            [],
            body,
            true,
            body
         ),
         PrimaryCountry.CountryName
      );

      Assert.Equal(20, matches.Count);
   }

   [Fact]
   public void FindPageMatchesSkipsOverlappingTextSnippets()
   {
      var body =
         "Entry action | Target | Target TOWNSEND, Hugo | " +
         "Stockholms GK | 16 Entry action | Target | Target " +
         "JONSSON, Tobias | Haninge Strand GK";
      var matches = LlamaPageToolFormatter.FindPageMatches(
         new WebPageContent(
            "Article Title",
            "https://example.test/roster",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            [],
            body,
            true,
            body
         ),
         "Target"
      );

      Assert.Equal(2, matches.Count);
      Assert.All(
         matches,
         match => Assert.True(match.Snippet.Length < 180)
      );
   }

   [Fact]
   public void FormatPageContentTextBreaksLongPipeSeparatedRows()
   {
      var output = LlamaPageToolFormatter.FormatPageContentText(
         "Page URL",
         "https://example.test/entry-list",
         "Entry List",
         "https://example.test/entry-list",
         null,
         null,
         null,
         null,
         null,
         null,
         $"{PrimaryCountry.CountryName} | LAGERGREN, Joakim | " +
         "Black Mountain GC | 24"
      );

      Assert.Contains(
         $"{PrimaryCountry.CountryName} |\nLAGERGREN, Joakim |\n" +
         "Black Mountain GC |",
         output,
         StringComparison.Ordinal
      );
   }

   [Fact]
   public void FormatPageContentTextIncludesHighlightedRows()
   {
      var output = LlamaPageToolFormatter.FormatPageContentText(
         "Page URL",
         "https://example.test/entry-list",
         "Entry List",
         "https://example.test/entry-list",
         null,
         null,
         null,
         null,
         "Detected rows for Target",
         ["Target Player | Club | 24"],
         "Full page text."
      );

      Assert.Contains("Detected rows for Target:", output);
      Assert.Contains("Count: 1", output);
      Assert.Contains("- Target Player | Club | 24", output);
   }

   [Fact]
   public void FormatPageContentTextOmitsEmptyHighlightedRows()
   {
      var output = LlamaPageToolFormatter.FormatPageContentText(
         "Page URL",
         "https://example.test/entry-list",
         "Entry List",
         "https://example.test/entry-list",
         null,
         null,
         null,
         null,
         "Detected rows for Target",
         [],
         "Full page text."
      );

      Assert.DoesNotContain("Detected rows for Target", output);
      Assert.DoesNotContain("Count: 0", output);
      Assert.Contains("Page text:", output);
   }

   [Fact]
   public void ExtractMatchingRowsReturnsCompactMatchingRows()
   {
      var body =
         "Target LAGERGREN, Joakim | Black Mountain GC | 24\n" +
         "Other OTHER, One | Club | 99\n" +
         "Target DANTORP, Jens | Hills G&SC | 85\n" +
         "Target NOREN, AlexTroon | Troon | 60\n" +
         "Target FORSSTRÖM, SimonGamebook | Gamebook | 11";
      var rows = LlamaPageToolFormatter.ExtractMatchingRows(
         body,
         "Target",
         50
      );

      Assert.Equal(4, rows.Count);
      Assert.Contains(
         "Target LAGERGREN, Joakim | Black Mountain GC | 24",
         rows
      );
      Assert.Contains(
         "Target DANTORP, Jens | Hills G&SC | 85",
         rows
      );
      Assert.Contains(
         "Target NOREN, Alex | Troon | 60",
         rows
      );
      Assert.Contains(
         "Target FORSSTRÖM, Simon | Gamebook | 11",
         rows
      );
   }

   [Fact]
   public void ExtractMatchingRowsAcceptsMultipleMatchingTerms()
   {
      var body =
         $"{PrimaryCountry.CountryName} Player One | Club A | 1\n" +
         $"{PrimaryCountry.LocalDisplayName} Player Two | Club B | 2\n" +
         "Other Player Three | Club C | 3";
      var rows = LlamaPageToolFormatter.ExtractMatchingRows(
         body,
         [
            PrimaryCountry.CountryName,
            PrimaryCountry.LocalDisplayName
         ],
         50
      );

      Assert.Equal(2, rows.Count);
      Assert.Contains(
         $"{PrimaryCountry.CountryName} Player One | Club A | 1",
         rows
      );
      Assert.Contains(
         $"{PrimaryCountry.LocalDisplayName} Player Two | Club B | 2",
         rows
      );
   }

   [Fact]
   public void ExtractMatchingRowsClipsLongPipeDelimitedRows()
   {
      var body =
         $"Action | {PrimaryCountry.CountryName} LINDBERG, Mikael | " +
         "Svartinge GC | " +
         "5 Action | Austria WIESBERGER, Bernd | Club B | " +
         "6 Action | United States GUMBERG, Jordan | Club C | " +
         $"8 Action | {PrimaryCountry.CountryName} SVENSSON, Jesper | " +
         "Upsala GC | " +
         "48 Action | Japan HOSHINO, Rikuya | Club D | 50";
      var rows = LlamaPageToolFormatter.ExtractMatchingRows(
         body,
         PrimaryCountry.CountryName,
         50
      );

      Assert.Equal(2, rows.Count);
      Assert.Contains(
         $"{PrimaryCountry.CountryName} LINDBERG, Mikael | " +
         "Svartinge GC | 5",
         rows
      );
      Assert.Contains(
         $"{PrimaryCountry.CountryName} SVENSSON, Jesper | " +
         "Upsala GC | 48",
         rows
      );
      Assert.DoesNotContain(
         rows,
         row => row.Contains("WIESBERGER", StringComparison.Ordinal)
      );
   }

   [Fact]
   public void ExtractMatchingRowsAcceptsStructuredCountryCodeRows()
   {
      var body =
         $"COLLET Thibaut | 5.88 | 5.95 {PrimaryCountry.ThreeLetterCode} | " +
         "DUPLANTIS Armand | 6.13 | 6.30 FRA | " +
         "MARSCHALL Kurtis | 5.95 | 6.05";
      var rows = LlamaPageToolFormatter.ExtractMatchingRows(
         body,
         [
            PrimaryCountry.CountryName,
            PrimaryCountry.LocalDisplayName,
            PrimaryCountry.ThreeLetterCode
         ],
         50
      );

      Assert.Single(rows);
      Assert.Contains(
         $"{PrimaryCountry.ThreeLetterCode} | DUPLANTIS Armand | 6.13",
         rows[0],
         StringComparison.Ordinal
      );
      Assert.DoesNotContain("COLLET", rows[0], StringComparison.Ordinal);
   }

   [Fact]
   public void ExtractMatchingRowsIgnoresCountryCodeInsideWords()
   {
      var body =
         PrimaryCountry.ThreeLetterCode +
         "ET weather note | strong winds expected | 12";
      var rows = LlamaPageToolFormatter.ExtractMatchingRows(
         body,
         [PrimaryCountry.ThreeLetterCode],
         50
      );

      Assert.Empty(rows);
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}",
         result.OutputText
      );
      Assert.Single(handler.RequestBodies);
      Assert.Contains(
         "\"response_format\":{\"type\":\"json_schema\"",
         handler.RequestBodies[0]
      );
      Assert.Contains(
         "Output format instructions:",
         handler.RequestBodies[0]
      );
      Assert.Contains(
         "\\u0022type\\u0022: \\u0022object\\u0022",
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
            $"{PrimaryCountry.CountryName} lineup info."
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
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
      Assert.Contains(
         "\"response_format\":{\"type\":\"json_schema\"",
         handler.RequestBodies[1]
      );
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}",
         result.OutputText
      );
      Assert.Equal(2, handler.RequestBodies.Count);
      Assert.Contains(
         "Return only one raw object literal.",
         handler.RequestBodies[1]
      );
      Assert.Contains(
         "\"response_format\":{\"type\":\"json_schema\"",
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
         "Return only one raw object literal.",
         result.ToolTraceJson
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncContinuesToolsAfterInvalidToolFinal()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaInvalidFinalResponseJson(),
         CreateLlamaPageCallResponseJson(),
         CreateLlamaFinalResponseJson()
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/roster",
            $"{PrimaryCountry.CountryName} lineup info."
         )
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         webSearchClient,
         new RecordingWebPageContentClient(
            new WebPageContent(
               "Roster",
               "https://example.test/roster",
               null,
               [],
               $"{PrimaryCountry.CountryName} roster text.",
               true
            )
         ),
         new NoopLogger<LlamaServerClient>()
      );

      var result = await client.GenerateAsync(
         CreateProvider("llama-server"),
         CreateJob("json_schema", true, CreateToolsJson()),
         CreatePrompt(CreateParticipationSchemaJson()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}",
         result.OutputText
      );
      Assert.Equal(4, handler.RequestBodies.Count);
      Assert.DoesNotContain(
         "\"response_format\"",
         handler.RequestBodies[0]
      );
      Assert.DoesNotContain(
         "\"response_format\"",
         handler.RequestBodies[1]
      );
      Assert.Contains("\"tools\":[", handler.RequestBodies[2]);
      Assert.Contains("\"tool_choice\":\"required\"", handler.RequestBodies[2]);
      Assert.Contains(
         "previous final answer was rejected",
         handler.RequestBodies[2]
      );
      Assert.Contains("\"kind\":\"validation_feedback\"", result.ToolTraceJson);
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncRetriesWhenFinalOutputViolatesSchema()
   {
      var handler = new RecordingHandler(
         CreateLlamaFinalResponseJson(
            "{\"Participation\":\"Unknown\","
            + "\"Participants\":[],"
            + "\"Sources\":[]}"
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
         CreateJob("json_schema", requiresWebSearch: false, null),
         CreatePrompt(CreateParticipationSchemaJsonWithRequiredSource()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestBodies.Count);
      Assert.Contains(
         "Return only one raw object literal.",
         handler.RequestBodies[1]
      );
      Assert.Contains(
         "\"validation_status\":\"rejected\"",
         result.ToolTraceJson
      );
      Assert.Equal(
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}",
         result.OutputText
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncRetriesWhenParticipationYesHasNoParticipants()
   {
      var handler = new RecordingHandler(
         CreateLlamaFinalResponseWithContentJson(
            CreateParticipationCheckedOutput(
               "Yes",
               "https://example.test/roster",
               AiParticipationEvidenceTypeIds.EventInfoOnly
            )
         ),
         CreateLlamaParticipationFinalResponseJson()
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
            jobId: AiJobIds.DecidePrimaryCountryParticipation
         ),
         CreatePrompt(CreateParticipationSchemaJsonWithEvidenceType()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestBodies.Count);
      Assert.Contains(
         "\"validation_status\":\"rejected\"",
         result.ToolTraceJson
      );
      Assert.Equal(
         CreateParticipationYesOutput("https://example.test/roster"),
         result.OutputText
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncRetriesWhenParticipationSourceWasNotFetched()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaPageCallResponseJson(),
         CreateLlamaParticipationFinalResponseJson(
            "https://example.test/other"
         ),
         CreateLlamaParticipationFinalResponseJson()
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor roster",
            "https://example.test/roster",
            $"{PrimaryCountry.CountryName} lineup info."
         )
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Article Title",
            "https://example.test/roster",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Article heading"],
            $$"""
            Entry list
            Bib | Name | Country
            1 | Dino Beganovic | {{PrimaryCountry.CountryName}}
            2 | Alex Driver | Nation A
            3 | Casey Rider | Nation B
            """,
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
            requiresWebSearch: true,
            toolsJson: CreateToolsJson(),
            jobId: AiJobIds.DecidePrimaryCountryParticipation
         ),
         CreatePrompt(CreateParticipationSchemaJsonWithEvidenceType()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(4, handler.RequestBodies.Count);
      Assert.Contains(
         "\"validation_status\":\"rejected\"",
         result.ToolTraceJson
      );
      Assert.Equal(
         CreateParticipationYesOutput("https://example.test/roster"),
         result.OutputText
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncRetriesWhenNoUsesWeakEvidence()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaPageCallResponseJson("https://example.test/participants"),
         CreateLlamaFindPageCallResponseJson(
            "https://example.test/participants"
         ),
         CreateLlamaFinalResponseWithContentJson(
            CreateParticipationCheckedOutput(
               "No",
               "https://example.test/participants",
               AiParticipationEvidenceTypeIds.ParticipantList
            )
         ),
         CreateLlamaFinalResponseWithContentJson(
            CreateParticipationCheckedOutput(
               "Unknown",
               "https://example.test/participants",
               AiParticipationEvidenceTypeIds.EventInfoOnly
            )
         )
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor event info",
            "https://example.test/participants",
            "Event timetable info."
         )
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Event Timetable",
            "https://example.test/participants",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Schedule"],
            "Local time. Programme. Timetable.",
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
            requiresWebSearch: true,
            toolsJson: CreateToolsJson(),
            jobId: AiJobIds.DecidePrimaryCountryParticipation
         ),
         CreatePrompt(CreateParticipationSchemaJsonWithEvidenceType()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(5, handler.RequestBodies.Count);
      Assert.Contains(
         "\"validation_status\":\"rejected\"",
         result.ToolTraceJson
      );
      Assert.Equal(
         1,
         CountOccurrences(
            result.ToolTraceJson!,
            "\"validation_status\":\"rejected\""
         )
      );
      Assert.Equal(
         CreateParticipationCheckedOutput(
            "Unknown",
            "https://example.test/participants",
            AiParticipationEvidenceTypeIds.EventInfoOnly
         ),
         result.OutputText
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncRejectsUnknownWithoutTargetCountryCheck()
   {
      var unknownOutput =
         CreateParticipationCheckedOutput(
            "Unknown",
            "https://example.test/participants",
            AiParticipationEvidenceTypeIds.EventInfoOnly
         );
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaPageCallResponseJson("https://example.test/participants"),
         CreateLlamaFinalResponseWithContentJson(unknownOutput),
         CreateLlamaFinalResponseWithContentJson(unknownOutput),
         CreateLlamaFinalResponseWithContentJson(unknownOutput),
         CreateLlamaFinalResponseWithContentJson(unknownOutput)
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor event info",
            "https://example.test/participants",
            "Event timetable info."
         )
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Event Timetable",
            "https://example.test/participants",
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Schedule"],
            "Local time. Programme. Timetable.",
            true
         )
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         webSearchClient,
         webPageContentClient,
         new NoopLogger<LlamaServerClient>()
      );

      var exception = await Assert.ThrowsAsync<AiProviderExecutionException>(
         () => client.GenerateAsync(
            CreateProvider("llama-server"),
            CreateJob(
               "json_schema",
               requiresWebSearch: true,
               toolsJson: CreateToolsJson(),
               jobId: AiJobIds.DecidePrimaryCountryParticipation
            ),
            CreatePrompt(
               CreateParticipationSchemaJsonWithEvidenceType(),
               maxToolRounds: 4
            ),
            CreateRenderedPrompt(),
            "{}",
            CancellationToken.None
         )
      );

      Assert.Contains("target-country", exception.Message);
      Assert.Equal(
         2,
         CountOccurrences(
            exception.ToolTraceJson!,
            "\"kind\":\"validation_feedback\""
         )
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncRejectsNoWithStartListIndexPage()
   {
      var sourceUrl =
         "https://example.test/final-start-lists-event-index";
      var pdfUrl = "https://example.test/files/men-pole-vault.pdf";
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaPageCallResponseJson(sourceUrl),
         CreateLlamaFindPageCallResponseJson(sourceUrl),
         CreateLlamaFinalResponseWithContentJson(
            CreateParticipationCheckedOutput(
               "No",
               sourceUrl,
               AiParticipationEvidenceTypeIds.ParticipantList
            )
         ),
         CreateLlamaPageCallResponseJson(pdfUrl),
         CreateLlamaFinalResponseWithContentJson(
            CreateParticipationCheckedOutput(
               "Unknown",
               sourceUrl,
               AiParticipationEvidenceTypeIds.EventInfoOnly
            )
         )
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Final start lists event index",
            sourceUrl,
            "Links to event-specific start lists."
         )
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Final Start Lists: Grand Prix",
            sourceUrl,
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["START LISTS"],
            """
            Final Start Lists: Grand Prix
            View the final start lists below.
            START LISTS
            TIME | EVENT START LISTS PDF 16:00 | Hammer Throw
            16:05 | Long Jump
            16:30 | Pole Vault
            """,
            true,
            RelevantLinks:
            [
               new WebPageRelevantLink(
                  "Pole Vault- men",
                  pdfUrl
               )
            ]
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
            requiresWebSearch: true,
            toolsJson: CreateToolsJson(),
            jobId: AiJobIds.DecidePrimaryCountryParticipation
         ),
         CreatePrompt(CreateParticipationSchemaJsonWithEvidenceType()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(6, handler.RequestBodies.Count);
      Assert.Contains(
         "\"validation_status\":\"rejected\"",
         result.ToolTraceJson
      );
      Assert.Contains("\"kind\":\"validation_feedback\"", result.ToolTraceJson);
      Assert.Contains("PDF links:", result.ToolTraceJson);
      Assert.Contains(pdfUrl, webPageContentClient.Urls);
      Assert.Contains("\"tools\":[", handler.RequestBodies[4]);
      Assert.Contains(
         "previous final answer was rejected",
         handler.RequestBodies[4]
      );
      Assert.Equal(
         CreateParticipationCheckedOutput(
            "Unknown",
            sourceUrl,
            AiParticipationEvidenceTypeIds.EventInfoOnly
         ),
         result.OutputText
      );
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncAcceptsNoWithParticipantRows()
   {
      var sourceUrl = "https://example.test/final-start-list";
      var output =
         CreateParticipationCheckedOutput(
            "No",
            sourceUrl,
            AiParticipationEvidenceTypeIds.ParticipantList
         );
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaPageCallResponseJson(sourceUrl),
         CreateLlamaFindPageCallResponseJson(sourceUrl),
         CreateLlamaFinalResponseWithContentJson(output)
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Final start list",
            sourceUrl,
            "Participant rows."
         )
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "Final Start List",
            sourceUrl,
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            ["Entry List"],
            """
            Entry list
            Bib | Name | Country
            1 | Alex Runner | Nation A
            2 | Blake Jumper | Nation B
            3 | Casey Thrower | Nation C
            """,
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
            requiresWebSearch: true,
            toolsJson: CreateToolsJson(),
            jobId: AiJobIds.DecidePrimaryCountryParticipation
         ),
         CreatePrompt(CreateParticipationSchemaJsonWithEvidenceType()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(4, handler.RequestBodies.Count);
      Assert.DoesNotContain(
         "\"validation_status\":\"rejected\"",
         result.ToolTraceJson
      );
      Assert.Equal(output, result.OutputText);
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncCorrectsFinalReportAfterToolBudget()
   {
      var sourceUrl =
         "https://example.test/news/line-up-is-getting-stronger";
      var participantName = "Thobias Montler";
      var acceptedOutput =
         "{\"Participation\":\"Unknown\","
         + "\"Participants\":[],"
         + "\"CheckedSources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"EventInfoOnly\"}]}";
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(),
         CreateLlamaPageCallResponseJson(sourceUrl),
         CreateLlamaFindPageCallResponseJson(sourceUrl),
         CreateLlamaFinalResponseWithContentJson(
            "{\"Participation\":\"Yes\","
            + "\"Participants\":[{\"Name\":\"" + participantName + "\","
            + "\"Sources\":[{\"Url\":\"" + sourceUrl + "\","
            + "\"EvidenceType\":\"ParticipantList\"}]}],"
            + "\"CheckedSources\":[]}"
         ),
         CreateLlamaFinalResponseWithContentJson(
            "{\"Participation\":\"Unknown\","
            + "\"Participants\":[],"
            + "\"CheckedSources\":[{\"Url\":\"" + sourceUrl + "\","
            + "\"EvidenceType\":\"EventInfoOnly\"}]}"
         )
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Line-up announcement",
            sourceUrl,
            "Article mentioning one target-country athlete."
         )
      );
      var webPageContentClient = new RecordingWebPageContentClient(
         new WebPageContent(
            "The line-up is getting stronger",
            sourceUrl,
            DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
            [],
            $$"""
            The long jump field has added several athletes.
            It will include Jamaican jumpers, alongside
            {{PrimaryCountry.CountryName}}'s {{participantName}}.
            More announcements will follow.
            """,
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
            requiresWebSearch: true,
            toolsJson: CreateToolsJson(),
            jobId: AiJobIds.DecidePrimaryCountryParticipation
         ),
         CreatePrompt(
            CreateParticipationSchemaJsonWithEvidenceType(),
            maxToolRounds: 3
         ),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(5, handler.RequestBodies.Count);
      Assert.Contains(
         "\"validation_status\":\"rejected\"",
         result.ToolTraceJson
      );
      Assert.DoesNotContain("\"tools\":[", handler.RequestBodies[3]);
      Assert.DoesNotContain("\"tools\":[", handler.RequestBodies[4]);
      Assert.Contains(
         "previous final report was rejected",
         handler.RequestBodies[4]
      );
      Assert.Contains(
         "Preserve all participants",
         handler.RequestBodies[4]
      );
      Assert.Equal(acceptedOutput, result.OutputText);
   }

   [Fact]
   public void AiJobOutputValidatorAcceptsParticipantMention()
   {
      var sourceUrl = "https://example.test/news/line-up";
      var participantName = "Thobias Montler";
      var output =
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[{\"Name\":\"" + participantName + "\","
         + "\"Sources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"ParticipantMention\"}]}],"
         + "\"CheckedSources\":[]}";

      var result = AiJobOutputValidator.Validate(
         output,
         CreateJob(jobId: AiJobIds.DecidePrimaryCountryParticipation),
         true,
         CreateArticleMentionToolTrace(sourceUrl, participantName)
      );

      Assert.Equal(output, result);
   }

   [Fact]
   public void AiJobOutputValidatorNormalizesCheckedSourceEvidenceType()
   {
      var sourceUrl = "https://example.test/news/line-up";
      var participantName = "Thobias Montler";
      var output = CreateParticipationYesOutput(
         sourceUrl,
         AiParticipationEvidenceTypeIds.ParticipantMention,
         participantName
      );

      var result = AiJobOutputValidator.Validate(
         output,
         CreateJob(jobId: AiJobIds.DecidePrimaryCountryParticipation),
         true,
         CreateArticleMentionToolTrace(sourceUrl, participantName)
      );

      using var document = JsonDocument.Parse(result);
      Assert.Equal(
         AiParticipationEvidenceTypeIds.ParticipantMention,
         document.RootElement
            .GetProperty("Participants")[0]
            .GetProperty("Sources")[0]
            .GetProperty("EvidenceType")
            .GetString()
      );
      Assert.Equal(
         AiParticipationEvidenceTypeIds.EventInfoOnly,
         document.RootElement
            .GetProperty("CheckedSources")[0]
            .GetProperty("EvidenceType")
            .GetString()
      );
   }

   [Fact]
   public void AiJobOutputValidatorAcceptsNicknameInParticipantMention()
   {
      var sourceUrl = "https://example.test/news/line-up";
      var participantName = "Armand Duplantis";
      var evidenceName = "Armand Mondo Duplantis";
      var output =
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[{\"Name\":\"" + participantName + "\","
         + "\"Sources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"ParticipantMention\"}]}],"
         + "\"CheckedSources\":[]}";

      var result = AiJobOutputValidator.Validate(
         output,
         CreateJob(jobId: AiJobIds.DecidePrimaryCountryParticipation),
         true,
         CreateArticleMentionToolTrace(sourceUrl, evidenceName)
      );

      Assert.Equal(output, result);
   }

   [Fact]
   public void AiJobOutputValidatorAcceptsTimedParticipantListRows()
   {
      var sourceUrl = "https://example.test/start-list.pdf";
      var participantName = "Kramer Andreas";
      var countryCode = PrimaryCountry.ThreeLetterCode;
      var output =
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[{\"Name\":\"" + participantName + "\","
         + "\"Sources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"ParticipantList\"}]}],"
         + "\"CheckedSources\":[]}";
      var toolTrace = new JsonArray
      {
         new JsonObject
         {
            ["name"] = WebToolNames.GetPage,
            ["url"] = sourceUrl,
            ["result"] = $$"""
               Page URL: {{sourceUrl}}
               Title: official_entry_list.xlsx
               URL: {{sourceUrl}}
               Page text:
               Chapple Samuel NED 1:44.88 1:44.88
               {{participantName}} {{countryCode}} 1:43.13 1:43.73
               Pattison Ben GBR 1:42.27 1:46.08
               """
         }
      };

      var result = AiJobOutputValidator.Validate(
         output,
         CreateJob(jobId: AiJobIds.DecidePrimaryCountryParticipation),
         true,
         toolTrace
      );

      Assert.Equal(output, result);
   }

   [Fact]
   public void AiJobOutputValidatorAcceptsParticipantListWithReversedNameOrder()
   {
      var sourceUrl = "https://example.test/start-list.html";
      var participantName = "Peder Fredricson";
      var output =
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[{\"Name\":\"" + participantName + "\","
         + "\"Sources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"ParticipantList\"}]}],"
         + "\"CheckedSources\":[]}";
      var toolTrace = new JsonArray
      {
         new JsonObject
         {
            ["name"] = WebToolNames.GetPage,
            ["url"] = sourceUrl,
            ["result"] = $$"""
               Page URL: {{sourceUrl}}
               Title: Start list
               URL: {{sourceUrl}}
               Page text:
               SWE | FREDRICSON, Peder
               SWE | VON ECKERMANN, Henrik
               SWE | ROSLUND, Anita
               """
         }
      };

      var result = AiJobOutputValidator.Validate(
         output,
         CreateJob(jobId: AiJobIds.DecidePrimaryCountryParticipation),
         true,
         toolTrace
      );

      Assert.Equal(output, result);
   }

   [Fact]
   public void AiJobOutputValidatorAcceptsSingleRowParticipantListPage()
   {
      var sourceUrl = "https://example.test/players";
      var participantName = "Max Dahlin";
      var output =
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[{\"Name\":\"" + participantName + "\","
         + "\"Sources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"ParticipantList\"}]}],"
         + "\"CheckedSources\":[]}";
      var toolTrace = new JsonArray
      {
         new JsonObject
         {
            ["name"] = WebToolNames.GetPage,
            ["url"] = sourceUrl,
            ["result"] = $$"""
               Page URL: {{sourceUrl}}
               Title: Spelare - Nordea Open
               URL: {{sourceUrl}}
               Page text:
               {{participantName}}
               SWE
               """
         }
      };

      var result = AiJobOutputValidator.Validate(
         output,
         CreateJob(jobId: AiJobIds.DecidePrimaryCountryParticipation),
         true,
         toolTrace
      );

      Assert.Equal(output, result);
   }

   [Fact]
   public void AiJobOutputValidatorRejectsParticipantListForArticleMention()
   {
      var sourceUrl = "https://example.test/news/line-up";
      var participantName = "Thobias Montler";
      var output =
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[{\"Name\":\"" + participantName + "\","
         + "\"Sources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"ParticipantList\"}]}],"
         + "\"CheckedSources\":[]}";

      var exception = Assert.ThrowsAny<InvalidOperationException>(() =>
         AiJobOutputValidator.Validate(
            output,
            CreateJob(jobId: AiJobIds.DecidePrimaryCountryParticipation),
            true,
            CreateArticleMentionToolTrace(sourceUrl, participantName)
         )
      );

      Assert.Contains("Participant source EvidenceType", exception.Message);
   }

   [Fact]
   public async Task
      LlamaServerGenerateAsyncAcceptsUnknownWithSearchOnlyEvidence()
   {
      var handler = new RecordingHandler(
         CreateLlamaToolCallResponseJson(
            $"Tre Kronor {PrimaryCountry.CountryName}"
         ),
         CreateLlamaFinalResponseWithContentJson(
            CreateParticipationCheckedOutput(
               "Unknown",
               "https://example.test/search-result",
               AiParticipationEvidenceTypeIds.SearchOnly
            )
         )
      );
      var webSearchClient = new RecordingWebSearchClient(
         new WebSearchResult(
            "Tre Kronor event info",
            "https://example.test/search-result",
            "Search result only."
         )
      );
      var client = new LlamaServerClient(
         new HttpClient(handler),
         webSearchClient,
         new RecordingWebPageContentClient(null),
         new NoopLogger<LlamaServerClient>()
      );

      var result = await client.GenerateAsync(
         CreateProvider("llama-server"),
         CreateJob(
            "json_schema",
            requiresWebSearch: true,
            toolsJson: CreateToolsJson(),
            jobId: AiJobIds.DecidePrimaryCountryParticipation
         ),
         CreatePrompt(CreateParticipationSchemaJsonWithEvidenceType()),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestBodies.Count);
      Assert.DoesNotContain(
         "\"validation_status\":\"rejected\"",
         result.ToolTraceJson
      );
      Assert.Equal(
         CreateParticipationCheckedOutput(
            "Unknown",
            "https://example.test/search-result",
            AiParticipationEvidenceTypeIds.SearchOnly
         ),
         result.OutputText
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
      string? toolsJson = null,
      string? conditionalToolsJson = null,
      string jobId = "job"
   )
   {
      return new AiJobDefinition(
         jobId,
         "Job",
         null,
         "provider",
         outputMode,
         toolsJson,
         conditionalToolsJson,
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

   private static string CreateConditionalToolsJson()
   {
      return JsonSerializer.Serialize(
         new JsonArray
         {
            new JsonObject
            {
               ["when"] = new JsonObject
               {
                  ["prompt_output_schema_present"] = true
               },
               ["behavior"] = LlamaReportSubmission.ToolName,
               ["tools"] = new JsonArray
               {
                  new JsonObject
                  {
                     ["type"] = "function",
                     ["function"] = new JsonObject
                     {
                        ["name"] = LlamaReportSubmission.ToolName,
                        ["description"] =
                           "Submit the complete final report when research " +
                           "has identified at least one supported " +
                           "participant. Use web tools instead if no " +
                           "participant has been identified.",
                        ["parameters"] = new JsonObject
                        {
                           ["$ref"] = "prompt.output_schema"
                        }
                     }
                  }
               },
               ["tool_patches"] = new JsonArray
               {
                  new JsonObject
                  {
                     ["path"] =
                        "function.parameters.properties.Participants.minItems",
                     ["value"] = 1
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

   private static JsonArray CreateArticleMentionToolTrace(
      string sourceUrl,
      string participantName
   )
   {
      return
      [
         new JsonObject
         {
            ["name"] = WebToolNames.GetPage,
            ["url"] = sourceUrl,
            ["result"] = $$"""
               Page URL: {{sourceUrl}}
               Title: Line-up announcement
               URL: {{sourceUrl}}
               Page text:
               The long jump field has added several athletes.
               It will include Jamaican jumpers, alongside
               {{PrimaryCountry.CountryName}}'s {{participantName}}.
               More announcements will follow.
               """
         }
      ];
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

   private static string CreateLlamaToolCallResponseJson(
      string query = "Tre Kronor"
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
                  content = "",
                  tool_calls = new[]
                  {
                     new
                     {
                        id = "call_1",
                        type = "function",
                        function = new
                        {
                           name = WebToolNames.Search,
                           arguments = JsonSerializer.Serialize(new
                           {
                              query,
                              limit = 10
                           })
                        }
                     }
                  }
               },
               finish_reason = "tool_calls"
            },
         },
         model = "openai/gpt-4o-2024-08-06"
      });
   }

   private static string CreateLlamaSubmitReportResponseJson(string report)
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
                  content = "",
                  tool_calls = new[]
                  {
                     new
                     {
                        id = "call_report",
                        type = "function",
                        function = new
                        {
                           name = LlamaReportSubmission.ToolName,
                           arguments = report
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

   private static string CreateLlamaPageCallResponseJson(
      string url = "https://example.test/roster"
   )
   {
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
                              arguments = JsonSerializer.Serialize(new
                              {
                                 url
                              })
                           }
                        }
                     }
                  },
                  finish_reason = "tool_calls"
               }
            },
            model = "openai/gpt-4o-2024-08-06"
         }
      );
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
                           arguments = JsonSerializer.Serialize(new
                           {
                              url = "https://example.test/direct-page",
                              find = PrimaryCountry.CountryName
                           })
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
      return JsonSerializer.Serialize(new
      {
         choices = new[]
         {
            new
            {
               message = new
               {
                  role = "assistant",
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
                           arguments = JsonSerializer.Serialize(new
                           {
                              url = "https://example.test/roster",
                              find = PrimaryCountry.CountryName
                           })
                        }
                     }
                  }
               },
               finish_reason = "tool_calls"
            },
         },
         model = "openai/gpt-4o-2024-08-06"
      });
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

   private static string CreateLlamaFindPageCallResponseJson(
      string url = "https://example.test/roster",
      string find = PrimaryCountry.CountryName
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
                           arguments = JsonSerializer.Serialize(new
                           {
                              url,
                              find
                           })
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"" + sourceUrl + "\"]}";

      return CreateLlamaFinalResponseWithContentJson(content);
   }

   private static string CreateLlamaParticipationFinalResponseJson()
   {
      return CreateLlamaParticipationFinalResponseJson(
         "https://example.test/roster"
      );
   }

   private static string CreateLlamaParticipationFinalResponseJson(
      string sourceUrl
   )
   {
      return CreateLlamaFinalResponseWithContentJson(
         CreateParticipationYesOutput(sourceUrl)
      );
   }

   private static string CreateParticipationYesOutput(
      string sourceUrl,
      string evidenceType = AiParticipationEvidenceTypeIds.ParticipantList,
      string participantName = "Dino Beganovic"
   )
   {
      return
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[{\"Name\":\"" + participantName + "\","
         + "\"Sources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"" + evidenceType + "\"}]}],"
         + "\"CheckedSources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"" + evidenceType + "\"}]}";
   }

   private static string CreateParticipationCheckedOutput(
      string participation,
      string sourceUrl,
      string evidenceType
   )
   {
      return
         "{\"Participation\":\"" + participation + "\","
         + "\"Participants\":[],"
         + "\"CheckedSources\":[{\"Url\":\"" + sourceUrl + "\","
         + "\"EvidenceType\":\"" + evidenceType + "\"}]}";
   }

   private static string CreateLlamaFinalResponseWithContentJson(
      string content
   )
   {
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

   private static string CreateLlamaFinalResponseJsonWithReasoning(
      string reasoningContent
   )
   {
      var content =
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
         + "\"Sources\":[\"https://example.test/roster\"]}";

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
                     content,
                     reasoning_content = reasoningContent
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
         "{\"Participation\":\"Yes\","
         + "\"Participants\":[\"Dino Beganovic\"],"
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

   private static RecordingHandler.ResponseSpec CreatePegNativeFormatError()
   {
      return new RecordingHandler.ResponseSpec(
         HttpStatusCode.InternalServerError,
         "{"
         + "\"error\":{"
         + "\"code\":500,"
         + "\"message\":\"The model produced output that does not " +
         "match the expected peg-native format\","
         + "\"type\":\"server_error\""
         + "}"
         + "}"
      );
   }

   private static string CreateParticipationSchemaJson()
   {
      return """
      {
        "type": "object",
        "properties": {
          "Participation": {
            "type": "string"
          },
          "Participants": {
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
          "Participation",
          "Participants",
          "Sources"
        ],
        "additionalProperties": false
      }
      """;
   }

   private static string CreateParticipationSchemaJsonWithRequiredSource()
   {
      return """
      {
        "type": "object",
        "properties": {
          "Participation": {
            "type": "string"
          },
          "Participants": {
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
            },
            "minItems": 1
          }
        },
        "required": [
          "Participation",
          "Participants",
          "Sources"
        ],
        "additionalProperties": false
      }
      """;
   }

   private static string CreateParticipationSchemaJsonWithEvidenceType()
   {
      return """
      {
        "type": "object",
        "properties": {
          "Participation": {
            "type": "string"
          },
          "Participants": {
            "type": "array"
          },
          "CheckedSources": {
            "type": "array"
          }
        },
        "required": [
          "Participation",
          "Participants",
          "CheckedSources"
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

      public List<int> SearchAttempts { get; } = [];

      public Task<WebSearchResponse> SearchAsync(
         string query,
         int maxResults,
         CancellationToken cancellationToken,
         int searchAttempt = 0
      )
      {
         Queries.Add((query, maxResults));
         SearchAttempts.Add(searchAttempt);
         return Task.FromResult(new WebSearchResponse(
            results,
            "SearXNG/google",
            "engines=google"
         ));
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
