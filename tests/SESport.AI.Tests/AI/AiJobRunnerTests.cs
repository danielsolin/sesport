using SESport.AI.Clients;
using SESport.AI.Jobs;

using System.Text.Json.Nodes;

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
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var result = await runner.RunAsync(
         new AiJobRequest("job", """{"event":"test"}"""),
         CancellationToken.None
      );

      Assert.Equal("Failed run", result.ErrorMessage);
      Assert.Contains(WebToolNames.Search, result.ToolTraceJson);
      Assert.Equal("""{"request":"payload"}""", result.RawRequestJson);
      Assert.Equal("""{"error":"boom"}""", result.RawResponseJson);
      Assert.Equal(2, result.ToolRoundCount);
      Assert.Equal(8123, result.ConversationCharacterCount);
      Assert.Equal(
         "User",
         runRepository.StoredRun!.RenderedPrompt
      );
      Assert.Equal(
         "System",
         runRepository.StoredRun.RenderedSystemPrompt
      );
      Assert.NotNull(runRepository.UpdatedRun);
      Assert.Equal(AiJobRunStatus.Failed, runRepository.UpdatedRun!.Status);
      Assert.Contains(
         WebToolNames.Search,
         runRepository.UpdatedRun.ToolTraceJson
      );
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
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
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

   [Fact]
   public async Task RunAsyncRetriesCompletionPersistenceWithoutFailingRun()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new SuccessfulProviderClient();
      var runRepository = new RecordingRunRepository
      {
         UpdateFailuresRemaining = 1
      };
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var result = await runner.RunAsync(
         new AiJobRequest("job", "{\"event\":\"test\"}"),
         CancellationToken.None
      );

      Assert.Null(result.ErrorMessage);
      Assert.Equal(2, runRepository.UpdateCallCount);
      Assert.NotNull(runRepository.UpdatedRun);
      Assert.Equal(
         AiJobRunStatus.Completed,
         runRepository.UpdatedRun!.Status
      );
      Assert.Null(runRepository.UpdatedRun.ErrorMessage);
   }

   [Fact]
   public async Task RunAsyncAcceptsCompletionPersistedBeforeUpdateFailure()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new SuccessfulProviderClient();
      var runRepository = new RecordingRunRepository
      {
         AlwaysFailUpdates = true,
         PersistBeforeUpdateFailure = true
      };
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var result = await runner.RunAsync(
         new AiJobRequest("job", "{\"event\":\"test\"}"),
         CancellationToken.None
      );

      Assert.Null(result.ErrorMessage);
      Assert.Equal(3, runRepository.UpdateCallCount);
      Assert.NotNull(runRepository.UpdatedRun);
      Assert.Equal(
         AiJobRunStatus.Completed,
         runRepository.UpdatedRun!.Status
      );
   }

   [Fact]
   public async Task RunAsyncAcceptsFailurePersistedBeforeUpdateFailure()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new ThrowingProviderClient();
      var runRepository = new RecordingRunRepository
      {
         AlwaysFailUpdates = true,
         PersistBeforeUpdateFailure = true
      };
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var result = await runner.RunAsync(
         new AiJobRequest("job", "{\"event\":\"test\"}"),
         CancellationToken.None
      );

      Assert.Equal("Failed run", result.ErrorMessage);
      Assert.Equal(3, runRepository.UpdateCallCount);
      Assert.NotNull(runRepository.UpdatedRun);
      Assert.Equal(
         AiJobRunStatus.Failed,
         runRepository.UpdatedRun!.Status
      );
   }

   [Fact]
   public async Task ProcessRunAsyncDoesNotFailWhenCompletionStateIsUnknown()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new SuccessfulProviderClient();
      var runRepository = new RecordingRunRepository
      {
         AlwaysFailUpdates = true
      };
      var executionGate = new AiJobExecutionGate();
      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var runId = await runner.QueueAsync(
         new AiJobRequest("job", "{\"event\":\"test\"}"),
         CancellationToken.None
      );

      await runner.ProcessRunAsync(runId, CancellationToken.None);

      Assert.Equal(3, runRepository.UpdateCallCount);
      Assert.False(runRepository.FailRunCalled);
      Assert.Null(runRepository.UpdatedRun);
   }

   [Fact]
   public async Task RunAsyncDoesNotReturnCompletedWhenUpdateMissesRun()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new SuccessfulProviderClient();
      var runRepository = new RecordingRunRepository
      {
         UpdateReturnsFalse = true
      };
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var exception = await Assert.ThrowsAnyAsync<Exception>(
         () => runner.RunAsync(
            new AiJobRequest("job", "{\"event\":\"test\"}"),
            CancellationToken.None
         )
      );

      Assert.Contains(
         "Unable to persist completed AI run",
         exception.Message
      );
      Assert.Equal(3, runRepository.UpdateCallCount);
      Assert.Null(runRepository.UpdatedRun);
   }

   [Fact]
   public async Task ProcessRunAsyncKeepsUnknownFailureStateUnchanged()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new ThrowingProviderClient();
      var runRepository = new RecordingRunRepository
      {
         AlwaysFailUpdates = true
      };
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var runId = await runner.QueueAsync(
         new AiJobRequest("job", "{\"event\":\"test\"}"),
         CancellationToken.None
      );

      await runner.ProcessRunAsync(runId, CancellationToken.None);

      Assert.Equal(3, runRepository.UpdateCallCount);
      Assert.False(runRepository.FailRunCalled);
      Assert.Null(runRepository.UpdatedRun);
   }

   [Fact]
   public async Task RunAsyncPersistsLatestToolRoundCountFromProgress()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new ProgressReportingProviderClient();
      var runRepository = new RecordingRunRepository();
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var result = await runner.RunAsync(
         new AiJobRequest("job", """{"event":"test"}"""),
         CancellationToken.None
      );

      Assert.Equal(2, result.ToolRoundCount);
      Assert.NotNull(runRepository.UpdatedRun);
      Assert.Equal(2, runRepository.UpdatedRun!.ToolRoundCount);
   }

   [Fact]
   public async Task RunAsyncUsesIndependentTokenForToolTracePersistence()
   {
      using var progressCancellation = new CancellationTokenSource();
      progressCancellation.Cancel();

      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new ProgressReportingProviderClient(
         progressCancellation.Token
      );
      var runRepository = new RecordingRunRepository();
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      await runner.RunAsync(
         new AiJobRequest("job", """{"event":"test"}"""),
         CancellationToken.None
      );

      Assert.Equal(
         CancellationToken.None,
         runRepository.ToolTraceCancellationToken
      );
   }

   [Fact]
   public async Task QueueAsyncStoresPendingRun()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var promptRenderer = new RecordingPromptRenderer();
      var providerClient = new SuccessfulProviderClient();
      var runRepository = new RecordingRunRepository();
      var executionGate = new AiJobExecutionGate();

      var runner = new AiJobRunner(
         jobRepository,
         promptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var runId = await runner.QueueAsync(
         new AiJobRequest("job", """{"event":"test"}"""),
         CancellationToken.None
      );

      Assert.NotEqual(Guid.Empty, runId);
      Assert.NotNull(runRepository.StoredRun);
      Assert.Equal(AiJobRunStatus.Pending, runRepository.StoredRun!.Status);
      Assert.Equal(runId, runRepository.StoredRun.Id);
      Assert.Null(runRepository.StoredRun.RawRequestJson);
      Assert.Equal("System", runRepository.StoredRun.RenderedSystemPrompt);
      Assert.Equal(
         AiDefaults.DefaultMaxOutputTokens,
         runRepository.StoredRun.MaxOutputTokens
      );
      Assert.Equal(
         ExecutionEnvironment.Current,
         runRepository.StoredRun.ExecutionEnvironment
      );
   }

   [Fact]
   public async Task QueueAsyncUsesJobModelOverride()
   {
      var jobRepository = new RecordingJobDefinitionRepository
      {
         JobModel = "translation-model"
      };
      var runRepository = new RecordingRunRepository();
      var runner = new AiJobRunner(
         jobRepository,
         new RecordingPromptRenderer(),
         [new SuccessfulProviderClient()],
         runRepository,
         new AiJobExecutionGate()
      );

      await runner.QueueAsync(
         new AiJobRequest("job", "{}"),
         CancellationToken.None
      );

      Assert.Equal(
         "translation-model",
         runRepository.StoredRun!.ProviderModel
      );
   }

   [Fact]
   public async Task ProcessRunAsyncRendersSystemPromptForStoredRun()
   {
      var jobRepository = new RecordingJobDefinitionRepository();
      var queuePromptRenderer = new RecordingPromptRenderer();
      var processPromptRenderer = new PrefixingPromptRenderer();
      var providerClient = new CapturingProviderClient();
      var runRepository = new RecordingRunRepository();
      var executionGate = new AiJobExecutionGate();

      var queueRunner = new AiJobRunner(
         jobRepository,
         queuePromptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var processRunner = new AiJobRunner(
         jobRepository,
         processPromptRenderer,
         [providerClient],
         runRepository,
         executionGate
      );

      var runId = await queueRunner.QueueAsync(
         new AiJobRequest("job", """{"event":"test"}"""),
         CancellationToken.None
      );

      await processRunner.ProcessRunAsync(runId, CancellationToken.None);

      Assert.NotNull(providerClient.RenderedPrompt);
      Assert.Equal(
         "System",
         providerClient.RenderedPrompt!.SystemPrompt
      );
      Assert.Equal(
         "User",
         providerClient.RenderedPrompt.UserPrompt
      );
   }

   private sealed class RecordingJobDefinitionRepository
      : IAiJobDefinitionRepository
   {
      public string? JobModel { get; set; }

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
               null,
               null,
               true,
               true,
               null,
               JobModel
            )
         );
      }

      public Task<AiPromptDefinition?> GetPromptAsync(
         Guid promptId,
         CancellationToken cancellationToken
      )
      {
         return GetActivePromptAsync(
            "job",
            cancellationToken
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

   private sealed class PrefixingPromptRenderer : IAiPromptRenderer
   {
      public AiRenderedPrompt Render(
         AiPromptDefinition prompt,
         string inputPayloadJson
      )
      {
         return new AiRenderedPrompt(
            $"Rendered {prompt.SystemPrompt}",
            $"Rendered {prompt.UserPromptTemplate}"
         );
      }
   }

   private sealed class ThrowingProviderClient : IAiProviderClient
   {
      public IReadOnlyCollection<string> Kinds => ["llama-server"];

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
         Func<string?, int, CancellationToken, Task>? toolTraceUpdated = null
      )
      {
         throw new AiProviderExecutionException(
            "Failed run",
            null,
            """{"request":"payload"}""",
            """{"error":"boom"}""",
            $$"""[{"kind":"tool","name":"{{WebToolNames.Search}}"}]""",
            2,
            8123
         );
      }
   }

   private sealed class SuccessfulProviderClient : IAiProviderClient
   {
      public IReadOnlyCollection<string> Kinds => ["llama-server"];

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
         Func<string?, int, CancellationToken, Task>? toolTraceUpdated = null
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

   private sealed class CapturingProviderClient : IAiProviderClient
   {
      public IReadOnlyCollection<string> Kinds => ["llama-server"];

      public AiRenderedPrompt? RenderedPrompt { get; private set; }

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
         Func<string?, int, CancellationToken, Task>? toolTraceUpdated = null
      )
      {
         RenderedPrompt = renderedPrompt;

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
                  "output_text": "Completed run"
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

   private sealed class ProgressReportingProviderClient
      : IAiProviderClient
   {
      private readonly CancellationToken progressCancellationToken;

      public ProgressReportingProviderClient(
         CancellationToken progressCancellationToken = default
      )
      {
         this.progressCancellationToken = progressCancellationToken;
      }

      public IReadOnlyCollection<string> Kinds => ["llama-server"];

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

      public async Task<AiJobResult> GenerateAsync(
         AiProviderDefinition provider,
         AiJobDefinition job,
         AiPromptDefinition prompt,
         AiRenderedPrompt renderedPrompt,
         string inputPayloadJson,
         CancellationToken cancellationToken,
         Func<string?, int, CancellationToken, Task>? toolTraceUpdated = null
      )
      {
         if(toolTraceUpdated is not null)
         {
            await toolTraceUpdated(
               """[{"kind":"tool","turn":1}]""",
               1,
               progressCancellationToken
            );
            await toolTraceUpdated(
               """[{"kind":"tool","turn":1},{"kind":"tool","turn":2}]""",
               2,
               progressCancellationToken
            );
         }

         return new AiJobResult(
            Guid.NewGuid(),
            job.Id,
            provider.Id,
            provider.Model,
            renderedPrompt.ToPromptText(),
            """{"request":"payload"}""",
            "Completed run",
            """
            {
               "output_text": "Completed run"
            }
            """,
            """[{"kind":"tool","turn":1},{"kind":"tool","turn":2}]""",
            2,
            48,
            null,
            null,
            null,
            null
         );
      }
   }

   private sealed class RecordingRunRepository : IAiJobRunRepository
   {
      public AiJobRun? StoredRun { get; private set; }

      public AiJobRun? UpdatedRun { get; private set; }

      public int UpdateFailuresRemaining { get; set; }

      public bool AlwaysFailUpdates { get; set; }

      public int UpdateCallCount { get; private set; }

      public bool FailRunCalled { get; private set; }

      public bool UpdateReturnsFalse { get; set; }

      public bool PersistBeforeUpdateFailure { get; set; }

      public CancellationToken ToolTraceCancellationToken
      {
         get;
         private set;
      }

      public Task StoreAsync(
         AiJobRun run,
         CancellationToken cancellationToken
      )
      {
         StoredRun = run;
         return Task.CompletedTask;
      }

      public Task RecordApplicationAsync(
         Guid runId,
         string targetType,
         string targetId,
         CancellationToken cancellationToken
      )
      {
         return Task.CompletedTask;
      }

      public Task<Guid?> GetExistingRunIdAsync(
         string jobId,
         string correlationId,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<Guid?>(null);
      }

      public Task<Guid?> GetActiveRunIdAsync(
         string jobId,
         string correlationId,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<Guid?>(null);
      }

      public Task<AiRunDetail?> GetRunAsync(
         Guid id,
         CancellationToken cancellationToken
      )
      {
         var run = UpdatedRun ?? StoredRun;

         if(run is null)
         {
            return Task.FromResult<AiRunDetail?>(null);
         }

         return Task.FromResult<AiRunDetail?>(
            new AiRunDetail(
               Id: run.Id,
               JobId: run.JobId,
               JobLabel: run.JobLabel,
               PromptId: run.PromptId,
               PromptVersion: run.PromptVersion,
               SystemPrompt: run.PromptSystemPrompt,
               UserPromptTemplate: run.PromptUserPromptTemplate,
               PromptTemperature: run.PromptTemperature,
               PromptMaxOutputTokens: run.PromptMaxOutputTokens,
               PromptMaxToolRounds: run.PromptMaxToolRounds,
               MaxOutputTokens: run.MaxOutputTokens,
               PromptOutputSchemaJson: run.PromptOutputSchemaJson,
               PromptRequestOptionsJson: run.PromptRequestOptionsJson,
               ProviderId: run.ProviderId,
               ProviderLabel: run.ProviderLabel,
               ProviderKind: run.ProviderKind,
               ProviderBaseAddress: run.ProviderBaseAddress,
               ProviderModel: run.ProviderModel,
               ProviderApiKeySource: run.ProviderApiKeySource,
               ProviderRequestOptionsJson: run.ProviderRequestOptionsJson,
               StatusId: run.Status switch
               {
                  AiJobRunStatus.Pending => "pending",
                  AiJobRunStatus.Running => "running",
                  AiJobRunStatus.Completed => "completed",
                  AiJobRunStatus.Failed => "failed",
                  AiJobRunStatus.Archived => "archived",
                  _ => "pending"
               },
               CorrelationId: run.CorrelationId,
               InputPayloadJson: run.InputPayloadJson,
               RenderedSystemPrompt: run.RenderedSystemPrompt,
               RenderedPrompt: run.RenderedPrompt,
               RawRequestJson: run.RawRequestJson,
               RawResponseJson: run.RawResponseJson,
               ToolTraceJson: run.ToolTraceJson,
               ToolRoundCount: run.ToolRoundCount,
               ConversationCharacterCount: run.ConversationCharacterCount,
               OutputText: run.OutputText,
               ErrorMessage: run.ErrorMessage,
               StartedAt: run.StartedAt,
               CompletedAt: run.CompletedAt,
               DurationSeconds: run.DurationSeconds,
               InputTokens: run.InputTokens,
               OutputTokens: run.OutputTokens,
               ReasoningTokens: run.ReasoningTokens,
               ExecutionEnvironment: run.ExecutionEnvironment,
               JobOutputMode: run.JobOutputMode,
               JobRequiresWebSearch: run.JobRequiresWebSearch,
               JobToolsJson: run.JobToolsJson,
               JobConditionalToolsJson: run.JobConditionalToolsJson,
               JobToolCallMaxTokens: run.JobToolCallMaxTokens
            )
         );
      }

      public Task<AiRunReference?> GetOriginatingActivityRunAsync(
         Guid activityId,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<AiRunReference?>(null);
      }

      public Task<bool> TryClaimRunAsync(
         Guid id,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult(true);
      }

      public Task<AiJobRunClaim?> ClaimNextRunAsync(
         IReadOnlyCollection<string> busyProviderIds,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<AiJobRunClaim?>(null);
      }

      public Task DeleteRunAsync(
         Guid id,
         CancellationToken cancellationToken
      )
      {
         return Task.CompletedTask;
      }

      public Task<bool> ArchiveRunAsync(
         Guid id,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult(true);
      }

      public Task<bool> UnarchiveRunAsync(
         Guid id,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult(true);
      }

      public Task FailRunAsync(
         Guid id,
         string errorMessage,
         CancellationToken cancellationToken
      )
      {
         FailRunCalled = true;
         var run = UpdatedRun ?? StoredRun;

         if(run is not null)
         {
            UpdatedRun = run with
            {
               Status = AiJobRunStatus.Failed,
               ErrorMessage = errorMessage,
               CompletedAt = DateTimeOffset.UtcNow
            };
         }

         return Task.CompletedTask;
      }

      public Task<bool> UpdateAsync(
         AiJobRun run,
         CancellationToken cancellationToken
      )
      {
         UpdateCallCount++;

         if(AlwaysFailUpdates)
         {
            if(PersistBeforeUpdateFailure)
            {
               UpdatedRun = run;
            }

            throw new InvalidOperationException(
               "Run persistence is temporarily unavailable."
            );
         }

         if(UpdateFailuresRemaining > 0)
         {
            UpdateFailuresRemaining--;
            throw new InvalidOperationException(
               "Run persistence is temporarily unavailable."
            );
         }

         if(UpdateReturnsFalse)
         {
            return Task.FromResult(false);
         }

         UpdatedRun = run;
         return Task.FromResult(true);
      }

      public Task UpdateToolTraceAsync(
         Guid runId,
         string? toolTraceJson,
         int toolRoundCount,
         CancellationToken cancellationToken
      )
      {
         ToolTraceCancellationToken = cancellationToken;

         StoredRun = StoredRun is null
            ? null
            : StoredRun with
            {
               ToolTraceJson = toolTraceJson,
               ToolRoundCount = toolRoundCount
            };

         UpdatedRun = UpdatedRun is null
            ? null
            : UpdatedRun with
            {
               ToolTraceJson = toolTraceJson,
               ToolRoundCount = toolRoundCount
            };

         return Task.CompletedTask;
      }

      public Task<int> FailStaleRunningRunsAsync(
         TimeSpan maxAge,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult(0);
      }
   }
}
