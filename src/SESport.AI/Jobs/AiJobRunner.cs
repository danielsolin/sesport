using System.Text.Json;

using Microsoft.Extensions.Logging;

using SESport.AI.Clients;
using SESport.Core.AI;

namespace SESport.AI.Jobs;

public sealed class AiJobRunner(
   IAiJobDefinitionRepository definitionRepository,
   IAiPromptRenderer promptRenderer,
   IEnumerable<IAiProviderClient> providerClients,
   IAiJobRunRepository runRepository,
   AiJobExecutionGate executionGate,
   ILogger<AiJobRunner>? logger = null,
   AiPendingRunWakeSignal? pendingRunWakeSignal = null
) : IAiJobRunner, IAiJobProcessor
{
   public async Task<Guid> QueueAsync(
      AiJobRequest request,
      CancellationToken cancellationToken
   )
   {
      var context = await BuildExecutionContextAsync(
         request,
         cancellationToken
      );

      await runRepository.StoreAsync(context.Run, cancellationToken);
      pendingRunWakeSignal?.Notify();
      return context.Run.Id;
   }

   public async Task<AiJobResult> RunAsync(
      AiJobRequest request,
      CancellationToken cancellationToken
   )
   {
      var context = await BuildExecutionContextAsync(
         request,
         cancellationToken
      );

      await runRepository.StoreAsync(context.Run, cancellationToken);

      await executionGate.WaitAsync(
         context.Run.ProviderId,
         cancellationToken
      );
      try
      {
         if(await runRepository.TryClaimRunAsync(
            context.Run.Id,
            cancellationToken
         ))
         {
            return await ExecuteAsync(context, cancellationToken);
         }
      }
      finally
      {
         executionGate.Release(context.Run.ProviderId);
      }

      return await WaitForCompletionAsync(
         context.Run.Id,
         cancellationToken
      );
   }

   public async Task ProcessRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var context = await BuildExecutionContextAsync(
            runId,
            cancellationToken
         );

         await executionGate.WaitAsync(
            context.Run.ProviderId,
            cancellationToken
         );
         try
         {
            await ExecuteAsync(context, cancellationToken);
         }
         finally
         {
            executionGate.Release(context.Run.ProviderId);
         }
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(AiJobCompletionPersistenceException exception)
      {
         logger?.LogError(
            exception,
            "AI run {RunId} completed at the provider, but its final " +
            "database state could not be verified. The run was left " +
            "unchanged to avoid overwriting a possible completion.",
            runId
         );
      }
      catch(AiJobFailurePersistenceException exception)
      {
         logger?.LogError(
            exception,
            "AI run {RunId} failed, but its final failure state could " +
            "not be verified. The run was left unchanged.",
            runId
         );
      }
      catch(Exception exception)
      {
         logger?.LogError(
            exception,
            "AI run {RunId} failed before completion.",
            runId
         );
         await MarkRunFailedAsync(
            runId,
            exception.Message,
            cancellationToken
         );
      }
   }

   private async Task<AiJobResult> WaitForCompletionAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      while(true)
      {
         var run = await runRepository.GetRunAsync(
            runId,
            cancellationToken
         );

         if(run is null)
         {
            throw new InvalidOperationException(
               $"AI run '{runId}' does not exist."
            );
         }

         if(!string.Equals(
            run.StatusId,
            "pending",
            StringComparison.Ordinal
         ) &&
            !string.Equals(
               run.StatusId,
               "running",
               StringComparison.Ordinal
            ))
         {
            return MapResult(run);
         }

         await Task.Delay(
            AiWorkerDefaults.CompletionPollInterval,
            cancellationToken
         );
      }
   }

   private async Task<AiJobResult> ExecuteAsync(
      ExecutionContext context,
      CancellationToken cancellationToken
   )
   {
      var run = context.Run;

      run = run with
      {
         Status = AiJobRunStatus.Running,
         StartedAt = DateTimeOffset.UtcNow
      };

      async Task ReportToolTraceProgressAsync(
         string? toolTraceJson,
         int progressToolRoundCount,
         CancellationToken progressCancellationToken
      )
      {
         run = run with
         {
            ToolTraceJson = toolTraceJson,
            ToolRoundCount = progressToolRoundCount
         };

         try
         {
            // Trace persistence is best effort. Do not cancel an Npgsql
            // command with the provider process token while it is active.
            await runRepository.UpdateToolTraceAsync(
               run.Id,
               toolTraceJson,
               progressToolRoundCount,
               CancellationToken.None
            );
         }
         catch(Exception exception)
         {
            logger?.LogWarning(
               exception,
               "Unable to persist tool trace progress for AI run {RunId}.",
               run.Id
            );
         }
      }

      AiJobResult providerResult;

      try
      {
         providerResult = await context.ProviderClient.GenerateAsync(
            context.Provider,
            context.Job,
            context.Prompt,
            context.RenderedPrompt,
            context.InputPayloadJson,
            cancellationToken,
            ReportToolTraceProgressAsync
         );
      }
      catch(AiProviderExecutionException exception)
      {
         var failureTokenUsage = ExtractTokenUsage(
            exception.RawResponseJson
         );

         run = run with
         {
            Status = AiJobRunStatus.Failed,
            RawRequestJson = exception.RawRequestJson ?? run.RawRequestJson,
            RawResponseJson =
               exception.RawResponseJson ?? run.RawResponseJson,
            ToolTraceJson = exception.ToolTraceJson ?? run.ToolTraceJson,
            ToolRoundCount = exception.ToolRoundCount,
            ConversationCharacterCount =
               exception.ConversationCharacterCount,
            ErrorMessage = exception.Message,
            InputTokens = failureTokenUsage.inputTokens,
            OutputTokens = failureTokenUsage.outputTokens,
            ReasoningTokens = failureTokenUsage.reasoningTokens,
            CompletedAt = DateTimeOffset.UtcNow,
            DurationSeconds = (decimal)(
               DateTimeOffset.UtcNow - run.StartedAt
            ).TotalSeconds
         };

         await PersistFailedRunAsync(run, cancellationToken);

         return new AiJobResult(
            run.Id,
            run.JobId,
            run.ProviderId,
            run.ProviderModel,
            GetRenderedPromptText(
               run.RenderedSystemPrompt,
               run.RenderedPrompt
            ),
            run.RawRequestJson,
            run.OutputText ?? string.Empty,
            run.RawResponseJson,
            run.ToolTraceJson,
            run.ToolRoundCount,
            run.ConversationCharacterCount,
            failureTokenUsage.inputTokens,
            failureTokenUsage.outputTokens,
            failureTokenUsage.reasoningTokens,
            exception.Message
         );
      }
      catch(Exception exception)
      {
         run = run with
         {
            Status = AiJobRunStatus.Failed,
            ErrorMessage = exception.Message,
            CompletedAt = DateTimeOffset.UtcNow,
            DurationSeconds = (decimal)(
               DateTimeOffset.UtcNow - run.StartedAt
            ).TotalSeconds
         };

         await PersistFailedRunAsync(run, cancellationToken);

         return new AiJobResult(
            run.Id,
            run.JobId,
            run.ProviderId,
            run.ProviderModel,
            GetRenderedPromptText(
               run.RenderedSystemPrompt,
               run.RenderedPrompt
            ),
            run.RawRequestJson,
            run.OutputText ?? string.Empty,
            run.RawResponseJson,
            run.ToolTraceJson,
            run.ToolRoundCount,
            run.ConversationCharacterCount,
            null,
            null,
            null,
            exception.Message
         );
      }

      var tokenUsage = ExtractTokenUsage(
         providerResult.RawResponseJson
      );

      run = run with
      {
         Status = AiJobRunStatus.Completed,
         RawRequestJson = providerResult.RawRequestJson,
         RawResponseJson = providerResult.RawResponseJson,
         ToolTraceJson = providerResult.ToolTraceJson,
         ToolRoundCount = providerResult.ToolRoundCount,
         ConversationCharacterCount =
            providerResult.ConversationCharacterCount,
         OutputText = providerResult.OutputText,
         ErrorMessage = null,
         InputTokens = tokenUsage.inputTokens,
         OutputTokens = tokenUsage.outputTokens,
         ReasoningTokens = tokenUsage.reasoningTokens,
         CompletedAt = DateTimeOffset.UtcNow,
         DurationSeconds = (decimal)(
            DateTimeOffset.UtcNow - run.StartedAt
         ).TotalSeconds
      };

      await PersistCompletedRunAsync(run, cancellationToken);

      return new AiJobResult(
         run.Id,
         run.JobId,
         run.ProviderId,
         run.ProviderModel,
         GetRenderedPromptText(
            run.RenderedSystemPrompt,
            run.RenderedPrompt
         ),
         providerResult.RawRequestJson,
         run.OutputText ?? string.Empty,
         run.RawResponseJson,
         run.ToolTraceJson,
         run.ToolRoundCount,
         run.ConversationCharacterCount,
         tokenUsage.inputTokens,
         tokenUsage.outputTokens,
         tokenUsage.reasoningTokens,
         null
      );
   }

   private async Task PersistCompletedRunAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   )
   {
      try
      {
         await PersistRunWithRetryAsync(run, cancellationToken);
      }
      catch(AiRunPersistenceException exception)
      {
         if(await IsCompletedRunPersistedAsync(
            run.Id,
            cancellationToken
         ))
         {
            return;
         }

         throw new AiJobCompletionPersistenceException(
            run.Id,
            exception
         );
      }
   }

   private async Task PersistFailedRunAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   )
   {
      try
      {
         await PersistRunWithRetryAsync(run, cancellationToken);
      }
      catch(AiRunPersistenceException exception)
      {
         if(await IsFailedRunPersistedAsync(
            run.Id,
            cancellationToken
         ))
         {
            return;
         }

         throw new AiJobFailurePersistenceException(
            run.Id,
            exception
         );
      }
   }

   private async Task PersistRunWithRetryAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   )
   {
      Exception? lastException = null;

      for(
         var attempt = 1;
         attempt <= AiWorkerDefaults.RunPersistenceMaxAttempts;
         attempt++
      )
      {
         try
         {
            if(!await runRepository.UpdateAsync(run, cancellationToken))
            {
               throw new InvalidOperationException(
                  "AI run update did not affect an existing run."
               );
            }

            return;
         }
         catch(OperationCanceledException)
            when(cancellationToken.IsCancellationRequested)
         {
            throw;
         }
         catch(Exception exception)
         {
            lastException = exception;

            if(attempt >= AiWorkerDefaults.RunPersistenceMaxAttempts)
            {
               break;
            }

            var delay = GetRunPersistenceRetryDelay(attempt);

            logger?.LogWarning(
               exception,
               "Unable to persist AI run {RunId} on attempt {Attempt}. " +
               "Retrying in {Delay}.",
               run.Id,
               attempt,
               delay
            );

            await Task.Delay(delay, cancellationToken);
         }
      }

      throw new AiRunPersistenceException(
         run.Id,
         lastException!
      );
   }

   private async Task<bool> IsCompletedRunPersistedAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var persistedRun = await runRepository.GetRunAsync(
            runId,
            cancellationToken
         );

         return string.Equals(
            persistedRun?.StatusId,
            AiJobRunStatusIds.Completed,
            StringComparison.Ordinal
         );
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(Exception exception)
      {
         logger?.LogWarning(
            exception,
            "Unable to verify completion state for AI run {RunId}.",
            runId
         );
         return false;
      }
   }

   private async Task<bool> IsFailedRunPersistedAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var persistedRun = await runRepository.GetRunAsync(
            runId,
            cancellationToken
         );

         return string.Equals(
            persistedRun?.StatusId,
            AiJobRunStatusIds.Failed,
            StringComparison.Ordinal
         );
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(Exception exception)
      {
         logger?.LogWarning(
            exception,
            "Unable to verify failure state for AI run {RunId}.",
            runId
         );
         return false;
      }
   }

   private static TimeSpan GetRunPersistenceRetryDelay(int attempt)
   {
      var retryDelays = AiWorkerDefaults.RunPersistenceRetryDelays;

      if(attempt < 1 || attempt > retryDelays.Count)
      {
         return retryDelays[^1];
      }

      return retryDelays[attempt - 1];
   }

   private sealed class AiRunPersistenceException(
      Guid runId,
      Exception innerException
   ) : Exception(
      $"Unable to persist AI run '{runId}' after " +
      $"{AiWorkerDefaults.RunPersistenceMaxAttempts} attempts.",
      innerException
   );

   private sealed class AiJobCompletionPersistenceException(
      Guid runId,
      Exception innerException
   ) : Exception(
      $"Unable to persist completed AI run '{runId}'.",
      innerException
   );

   private sealed class AiJobFailurePersistenceException(
      Guid runId,
      Exception innerException
   ) : Exception(
      $"Unable to persist failed AI run '{runId}'.",
      innerException
   );

   private async Task<ExecutionContext> BuildExecutionContextAsync(
      AiJobRequest request,
      CancellationToken cancellationToken
   )
   {
      var job = await definitionRepository.GetJobAsync(
         request.JobId,
         cancellationToken
      );

      if(job is null || !job.Enabled)
      {
         throw new InvalidOperationException(
            $"AI job '{request.JobId}' does not exist."
         );
      }

      var prompt = await definitionRepository.GetActivePromptAsync(
         request.JobId,
         cancellationToken
      );

      if(prompt is null || !prompt.Enabled)
      {
         throw new InvalidOperationException(
            $"AI job '{request.JobId}' has no active prompt."
         );
      }

      var provider = await definitionRepository.GetProviderAsync(
         job.ProviderId,
         cancellationToken
      );

      if(provider is null || !provider.Enabled)
      {
         throw new InvalidOperationException(
            $"AI provider '{job.ProviderId}' does not exist."
         );
      }

      var effectiveModel = string.IsNullOrWhiteSpace(job.Model)
         ? provider.Model
         : job.Model.Trim();
      var effectiveProvider = provider with { Model = effectiveModel };
      var providerClient = GetProviderClient(provider.Kind);
      var renderedPrompt = promptRenderer.Render(
         prompt,
         request.InputPayloadJson
      );
      var maxOutputTokens = prompt.MaxOutputTokens ??
         AiDefaults.DefaultMaxOutputTokens;
      var maxToolRounds = job.RequiresWebSearch
         ? prompt.MaxToolRounds ?? LlamaServerDefaults.DefaultMaxToolRounds
         : prompt.MaxToolRounds;
      var run = new AiJobRun(
         Guid.NewGuid(),
         job.Id,
         prompt.Id,
         prompt.Version,
         prompt.SystemPrompt,
         prompt.UserPromptTemplate,
         provider.Id,
         effectiveModel,
         AiJobRunStatus.Pending,
         request.CorrelationId,
         request.InputPayloadJson,
         renderedPrompt.UserPrompt.Trim(),
         renderedPrompt.SystemPrompt?.Trim(),
         null!,
         null,
         null,
         0,
         0,
         null,
         null,
         DateTimeOffset.UtcNow,
         null,
         null,
         null,
         null,
         null,
         ExecutionEnvironment.Current,
         job.Label,
         provider.Label,
         job.OutputMode,
         job.RequiresWebSearch,
         job.ToolsJson,
         job.ConditionalToolsJson,
         job.ToolCallMaxTokens,
         provider.Kind,
         provider.BaseAddress,
         provider.ApiKeySource,
         provider.RequestOptionsJson,
         prompt.OutputSchemaJson,
         prompt.RequestOptionsJson,
         prompt.Temperature,
         prompt.MaxOutputTokens,
         maxToolRounds,
         maxOutputTokens,
         prompt.MinToolRounds,
         job.IncludeSocialMedia,
         prompt.CodexReasoningEffort
      );

      return new ExecutionContext(
         run,
         job,
         prompt,
         effectiveProvider,
         providerClient,
         renderedPrompt,
         request.InputPayloadJson
      );
   }

   private async Task<ExecutionContext> BuildExecutionContextAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      var run = await runRepository.GetRunAsync(
         runId,
         cancellationToken
      );

      if(run is null)
      {
         throw new InvalidOperationException(
            $"AI run '{runId}' does not exist."
         );
      }

      var job = CreateJobDefinition(run);
      var prompt = CreatePromptDefinition(run);
      var provider = CreateProviderDefinition(run);
      var providerClient = GetProviderClient(provider.Kind);
      var renderedPrompt = CreateRenderedPrompt(run, prompt);
      var runForExecution = run.ToAiJobRun();

      if(string.IsNullOrWhiteSpace(runForExecution.RenderedSystemPrompt))
      {
         runForExecution = runForExecution with
         {
            RenderedSystemPrompt = renderedPrompt.SystemPrompt?.Trim()
         };
      }

      return new ExecutionContext(
         runForExecution,
         job,
         prompt,
         provider,
         providerClient,
         renderedPrompt,
         run.InputPayloadJson
      );
   }

   private async Task MarkRunFailedAsync(
      Guid runId,
      string message,
      CancellationToken cancellationToken
   )
   {
      await runRepository.FailRunAsync(
         runId,
         message,
         cancellationToken
      );
   }

   private IAiProviderClient GetProviderClient(string kind)
   {
      var providerClient = providerClients.FirstOrDefault(client =>
         client.Kinds.Contains(kind)
      );

      if(providerClient is null)
      {
         throw new InvalidOperationException(
            $"No AI provider client is registered for kind '{kind}'."
         );
      }

      return providerClient;
   }

   private static AiJobResult MapResult(AiRunDetail run)
   {
      return new AiJobResult(
         run.Id,
         run.JobId,
         run.ProviderId,
         run.ProviderModel,
         GetRenderedPromptText(
            run.RenderedSystemPrompt,
            run.RenderedPrompt
         ),
         run.RawRequestJson ?? string.Empty,
         run.OutputText ?? string.Empty,
         run.RawResponseJson,
         run.ToolTraceJson,
         run.ToolRoundCount,
         run.ConversationCharacterCount,
         run.InputTokens,
         run.OutputTokens,
         run.ReasoningTokens,
         run.ErrorMessage
      );
   }

   private static (
      int? inputTokens,
      int? outputTokens,
      int? reasoningTokens
   ) ExtractTokenUsage(string? rawResponseJson)
   {
      if(string.IsNullOrWhiteSpace(rawResponseJson))
      {
         return (null, null, null);
      }

      try
      {
         using var document = JsonDocument.Parse(rawResponseJson);
         var root = document.RootElement;

         if(TryExtractTokenUsage(root, out var tokens))
         {
            return tokens;
         }

         if(root.TryGetProperty("usage", out var usageNode) &&
            usageNode.ValueKind == JsonValueKind.Object &&
            TryExtractTokenUsage(usageNode, out tokens))
         {
            return tokens;
         }
      }
      catch(Exception exception)
         when(exception is JsonException or ArgumentException)
      {
      }

      return (null, null, null);
   }

   private static bool TryExtractTokenUsage(
      JsonElement element,
      out (
         int? inputTokens,
         int? outputTokens,
         int? reasoningTokens
      ) tokens
   )
   {
      tokens = (null, null, null);

      var inputTokens = ReadIntProperty(
         element,
         "input_tokens",
         "prompt_tokens",
         "tokens_prompt"
      );
      var outputTokens = ReadIntProperty(
         element,
         "output_tokens",
         "completion_tokens",
         "tokens_completion"
      );
      var reasoningTokens = ReadIntProperty(
         element,
         "reasoning_tokens",
         "tokens_reasoning"
      );

      if(inputTokens is null &&
         outputTokens is null &&
         reasoningTokens is null)
      {
         return false;
      }

      tokens = (inputTokens, outputTokens, reasoningTokens);
      return true;
   }

   private static int? ReadIntProperty(
      JsonElement element,
      params string[] propertyNames
   )
   {
      foreach(var propertyName in propertyNames)
      {
         if(!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetInt32(out var value))
         {
            continue;
         }

         return value;
      }

      return null;
   }

   private sealed record ExecutionContext(
      AiJobRun Run,
      AiJobDefinition Job,
      AiPromptDefinition Prompt,
      AiProviderDefinition Provider,
      IAiProviderClient ProviderClient,
      AiRenderedPrompt RenderedPrompt,
      string InputPayloadJson
   );

   private AiRenderedPrompt CreateRenderedPrompt(
      AiRunDetail run,
      AiPromptDefinition prompt
   )
   {
      var renderedSystemPrompt = string.IsNullOrWhiteSpace(
         run.RenderedSystemPrompt
      )
         ? promptRenderer.Render(prompt, run.InputPayloadJson).SystemPrompt
         : run.RenderedSystemPrompt.Trim();

      return new AiRenderedPrompt(
         renderedSystemPrompt,
         run.RenderedPrompt
      );
   }

   private static AiJobDefinition CreateJobDefinition(AiRunDetail run)
   {
      return new AiJobDefinition(
         run.JobId,
         run.JobLabel,
         null,
         run.ProviderId,
         run.JobOutputMode,
         run.JobToolsJson,
         run.JobConditionalToolsJson,
         run.JobToolCallMaxTokens,
         run.JobRequiresWebSearch,
         true,
         null,
         IncludeSocialMedia: run.JobIncludeSocialMedia
      );
   }

   private static AiPromptDefinition CreatePromptDefinition(AiRunDetail run)
   {
      return new AiPromptDefinition(
         run.PromptId,
         run.JobId,
         run.PromptVersion,
         run.SystemPrompt,
         run.UserPromptTemplate,
         run.PromptOutputSchemaJson,
         run.PromptRequestOptionsJson,
         run.PromptTemperature,
         run.MaxOutputTokens,
         run.PromptMaxToolRounds,
         true,
         run.PromptMinToolRounds,
         run.PromptCodexReasoningEffort
      );
   }

   private static AiProviderDefinition CreateProviderDefinition(
      AiRunDetail run
   )
   {
      return new AiProviderDefinition(
         run.ProviderId,
         run.ProviderLabel,
         run.ProviderKind,
         run.ProviderBaseAddress,
         run.ProviderModel,
         run.ProviderApiKeySource,
         run.ProviderRequestOptionsJson,
         true
      );
   }

   private static string GetRenderedPromptText(
      string? renderedSystemPrompt,
      string renderedPrompt
   )
   {
      return new AiRenderedPrompt(
         renderedSystemPrompt,
         renderedPrompt
      ).ToPromptText();
   }
}

internal static class AiRunDetailExtensions
{
   public static AiJobRun ToAiJobRun(this AiRunDetail run)
   {
      return new AiJobRun(
         run.Id,
         run.JobId,
         run.PromptId,
         run.PromptVersion,
         run.SystemPrompt,
         run.UserPromptTemplate,
         run.ProviderId,
         run.ProviderModel,
         ToStatus(run.StatusId),
         run.CorrelationId,
         run.InputPayloadJson,
         run.RenderedPrompt,
         run.RenderedSystemPrompt,
         run.RawRequestJson ?? string.Empty,
         run.RawResponseJson,
         run.ToolTraceJson,
         run.ToolRoundCount,
         run.ConversationCharacterCount,
         run.OutputText,
         run.ErrorMessage,
         run.StartedAt,
         run.CompletedAt,
         run.DurationSeconds,
         run.InputTokens,
         run.OutputTokens,
         run.ReasoningTokens,
         ExecutionEnvironment.Current,
         run.JobLabel,
         run.ProviderLabel,
         run.JobOutputMode,
         run.JobRequiresWebSearch,
         run.JobToolsJson,
         run.JobConditionalToolsJson,
         run.JobToolCallMaxTokens,
         run.ProviderKind,
         run.ProviderBaseAddress,
         run.ProviderApiKeySource,
         run.ProviderRequestOptionsJson,
         run.PromptOutputSchemaJson,
         run.PromptRequestOptionsJson,
         run.PromptTemperature,
         run.PromptMaxOutputTokens,
         run.PromptMaxToolRounds,
         run.MaxOutputTokens,
         run.PromptMinToolRounds,
         run.JobIncludeSocialMedia,
         run.PromptCodexReasoningEffort
      );
   }

   private static AiJobRunStatus ToStatus(string statusId)
   {
      return statusId switch
      {
         "pending" => AiJobRunStatus.Pending,
         "running" => AiJobRunStatus.Running,
         "completed" => AiJobRunStatus.Completed,
         "failed" => AiJobRunStatus.Failed,
         "archived" => AiJobRunStatus.Archived,
         _ => AiJobRunStatus.Pending
      };
   }
}
