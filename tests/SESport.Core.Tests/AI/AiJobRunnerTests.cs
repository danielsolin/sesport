using System.Text.Json.Nodes;
using SESport.AI;
using SESport.AI.Abstractions;
using SESport.AI.Models;

namespace SESport.Core.Tests.AI;

public class AiJobRunnerTests
{
   [Fact]
   public async Task RunAsyncPersistsToolTraceForFailedRuns()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new ThrowingProviderClient();
      var runRepository = new RecordingRunRepository();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository
      );

      var result = await runner.RunAsync(
         new AiJobRequest("job", """{"event":"test"}"""),
         CancellationToken.None
      );

      Assert.Equal("Failed run", result.ErrorMessage);
      Assert.Contains("web_search", result.ToolTraceJson);
      Assert.Equal("""{"request":"payload"}""", result.RawRequestJson);
      Assert.Equal("""{"error":"boom"}""", result.RawResponseJson);
      Assert.Equal(2, result.ToolRoundCount);
      Assert.Equal(8123, result.ConversationCharacterCount);
      Assert.Contains(
         "Search the web and fetch the most relevant pages",
         runRepository.StoredRun!.RenderedPrompt
      );
      Assert.NotNull(runRepository.UpdatedRun);
      Assert.Equal(AiJobRunStatus.Failed, runRepository.UpdatedRun!.Status);
      Assert.Contains("web_search", runRepository.UpdatedRun.ToolTraceJson);
      Assert.Equal("""{"request":"payload"}""",
         runRepository.UpdatedRun.RawRequestJson);
      Assert.Equal("""{"error":"boom"}""",
         runRepository.UpdatedRun.RawResponseJson);
      Assert.Equal(2, runRepository.UpdatedRun.ToolRoundCount);
      Assert.Equal(8123, runRepository.UpdatedRun.ConversationCharacterCount);
      Assert.Equal("Failed run", runRepository.UpdatedRun.ErrorMessage);
   }

   [Fact]
   public async Task RunAsyncPersistsTokenUsageFromRawResponse()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new SuccessfulProviderClient();
      var runRepository = new RecordingRunRepository();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository
      );

      var result = await runner.RunAsync(
         new AiJobRequest("job", """{"event":"test"}"""),
         CancellationToken.None
      );

      Assert.Equal("Completed run", result.OutputText);
      Assert.Equal(12, result.InputTokens);
      Assert.Equal(34, result.OutputTokens);
      Assert.Equal(5, result.ReasoningTokens);
      Assert.Equal(0, result.ToolRoundCount);
      Assert.Equal(48, result.ConversationCharacterCount);
      Assert.NotNull(runRepository.UpdatedRun);
      Assert.Equal(12, runRepository.UpdatedRun!.InputTokens);
      Assert.Equal(34, runRepository.UpdatedRun.OutputTokens);
      Assert.Equal(5, runRepository.UpdatedRun.ReasoningTokens);
   }

   private sealed class RecordingJobDefinitionRepository
      : IAiJobDefinitionRepository
   {
      public Task<AiJobDefinition?> GetJobAsync(
         string jobId,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<AiJobDefinition?>(
            new AiJobDefinition(
               "job",
               "Job",
               null,
               "provider",
               "json_object",
               null,
               "Search the web and fetch the most relevant pages.",
               true,
               true,
               null
            )
         );
      }

      public Task<AiPromptDefinition?> GetActivePromptAsync(
         string jobId,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<AiPromptDefinition?>(
            new AiPromptDefinition(
               Guid.Parse("11111111-1111-1111-1111-111111111111"),
               "job",
               1,
               "System",
               "User",
               """{"type":"object"}""",
               "{}",
               null,
               null,
               true
            )
         );
      }

      public Task<AiProviderDefinition?> GetProviderAsync(
         string providerId,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<AiProviderDefinition?>(
            new AiProviderDefinition(
               "provider",
               "Provider",
               "llama-server",
               "http://127.0.0.1:1234/v1/",
               "gpt",
               "key:secret",
               "{}",
               true
            )
         );
      }
   }

   private sealed class RecordingPromptRenderer : IAiPromptRenderer
   {
      public AiRenderedPrompt Render(
         AiPromptDefinition prompt,
         string inputPayloadJson
      )
      {
         return new AiRenderedPrompt(
            prompt.SystemPrompt,
            prompt.UserPromptTemplate
         );
      }
   }

   private sealed class ThrowingProviderClient : IAiProviderClient
   {
      public string Kind => "llama-server";

      public JsonObject CreateRequestPayload(
         AiProviderDefinition provider,
         AiJobDefinition job,
         AiPromptDefinition prompt,
         AiRenderedPrompt renderedPrompt
      )
      {
         return new JsonObject
         {
            ["model"] = provider.Model,
            ["messages"] = new JsonArray()
         };
      }

      public Task<AiJobResult> GenerateAsync(
         AiProviderDefinition provider,
         AiJobDefinition job,
         AiPromptDefinition prompt,
         AiRenderedPrompt renderedPrompt,
         string inputPayloadJson,
         CancellationToken cancellationToken,
         Func<string?, CancellationToken, Task>? toolTraceUpdated = null
      )
      {
         throw new AiProviderExecutionException(
            "Failed run",
            null,
            """{"request":"payload"}""",
            """{"error":"boom"}""",
            """[{"kind":"tool","name":"web_search"}]""",
            2,
            8123
         );
      }
   }

   private sealed class SuccessfulProviderClient : IAiProviderClient
   {
      public string Kind => "llama-server";

      public JsonObject CreateRequestPayload(
         AiProviderDefinition provider,
         AiJobDefinition job,
         AiPromptDefinition prompt,
         AiRenderedPrompt renderedPrompt
      )
      {
         return new JsonObject
         {
            ["model"] = provider.Model,
            ["messages"] = new JsonArray()
         };
      }

      public Task<AiJobResult> GenerateAsync(
         AiProviderDefinition provider,
         AiJobDefinition job,
         AiPromptDefinition prompt,
         AiRenderedPrompt renderedPrompt,
         string inputPayloadJson,
         CancellationToken cancellationToken,
         Func<string?, CancellationToken, Task>? toolTraceUpdated = null
      )
      {
         return Task.FromResult(
            new AiJobResult(
               Guid.NewGuid(),
               job.Id,
               provider.Id,
               provider.Model,
               renderedPrompt.ToPromptText(),
               """{"request":"payload"}""",
               "Completed run",
               """
               {
                  "output_text": "Completed run",
                  "usage": {
                     "prompt_tokens": 12,
                     "completion_tokens": 34,
                     "reasoning_tokens": 5
                  }
               }
               """,
               null,
               0,
               48,
               null,
               null,
               null,
               null
            )
         );
      }
   }

   private sealed class RecordingRunRepository : IAiJobRunRepository
   {
      public AiJobRun? StoredRun { get; private set; }

      public AiJobRun? UpdatedRun { get; private set; }

      public Task StoreAsync(
         AiJobRun run,
         CancellationToken cancellationToken
      )
      {
         StoredRun = run;
         return Task.CompletedTask;
      }

      public Task UpdateAsync(
         AiJobRun run,
         CancellationToken cancellationToken
      )
      {
         UpdatedRun = run;
         return Task.CompletedTask;
      }

      public Task UpdateToolTraceAsync(
         Guid runId,
         string? toolTraceJson,
         CancellationToken cancellationToken
      )
      {
         StoredRun = StoredRun is null
            ? null
            : StoredRun with
            {
               ToolTraceJson = toolTraceJson
            };

         UpdatedRun = UpdatedRun is null
            ? null
            : UpdatedRun with
            {
               ToolTraceJson = toolTraceJson
            };

         return Task.CompletedTask;
      }
   }
}
