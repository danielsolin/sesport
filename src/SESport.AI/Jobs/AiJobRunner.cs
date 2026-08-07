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
   ILogger<AiJobRunner>? logger = null
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

      await executionGate.WaitAsync(cancellationToken);
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
         executionGate.Release();
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

         await ExecuteAsync(context, cancellationToken);
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(Exception exception)
      {
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
            await runRepository.UpdateToolTraceAsync(
               run.Id,
               toolTraceJson,
               progressToolRoundCount,
               progressCancellationToken
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

      try
      {
         var providerResult = await context.ProviderClient.GenerateAsync(
            context.Provider,
            context.Job,
            context.Prompt,
            context.RenderedPrompt,
            context.InputPayloadJson,
            cancellationToken,
            ReportToolTraceProgressAsync
         );
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
            InputTokens = tokenUsage.inputTokens,
            OutputTokens = tokenUsage.outputTokens,
            ReasoningTokens = tokenUsage.reasoningTokens,
            CompletedAt = DateTimeOffset.UtcNow,
            DurationSeconds = (decimal)(
               DateTimeOffset.UtcNow - run.StartedAt
            ).TotalSeconds
         };

         await runRepository.UpdateAsync(run, cancellationToken);

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
      catch(AiProviderExecutionException exception)
      {
         var tokenUsage = ExtractTokenUsage(exception.RawResponseJson);

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
            InputTokens = tokenUsage.inputTokens,
            OutputTokens = tokenUsage.outputTokens,
            ReasoningTokens = tokenUsage.reasoningTokens,
            CompletedAt = DateTimeOffset.UtcNow,
            DurationSeconds = (decimal)(
               DateTimeOffset.UtcNow - run.StartedAt
            ).TotalSeconds
         };

         await runRepository.UpdateAsync(run, cancellationToken);

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
            tokenUsage.inputTokens,
            tokenUsage.outputTokens,
            tokenUsage.reasoningTokens,
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

         await runRepository.UpdateAsync(run, cancellationToken);

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
   }

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
         job.IncludeSocialMedia
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
         string.Equals(client.Kind, kind, StringComparison.Ordinal)
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
      catch(JsonException)
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
         run.PromptMinToolRounds
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
         run.PromptMinToolRounds
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
