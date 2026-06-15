using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.AI.Providers;
using System.Text.Json;

namespace SESport.AI;

public sealed class AiJobRunner(
   IAiJobDefinitionRepository definitionRepository,
   IAiPromptRenderer promptRenderer,
   IEnumerable<IAiProviderClient> providerClients,
   IAiJobRunRepository runRepository
) : IAiJobRunner
{
   public async Task<AiJobResult> RunAsync(
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

      var providerClient = providerClients.FirstOrDefault(client =>
         string.Equals(client.Kind, provider.Kind, StringComparison.Ordinal)
      );

      if(providerClient is null)
      {
         throw new InvalidOperationException(
            $"No AI provider client is registered for kind '{provider.Kind}'."
         );
      }

      var renderedPrompt = promptRenderer.Render(
         prompt,
         request.InputPayloadJson
      );
      var requestPayload = providerClient.CreateRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt
      );
      var rawRequestJson = AiRequestJsonSerializer.Serialize(requestPayload);

      var run = new AiJobRun(
         Guid.NewGuid(),
         job.Id,
         prompt.Id,
         provider.Id,
         provider.Model,
         AiJobRunStatus.Running,
         request.CorrelationId,
         request.InputPayloadJson,
         renderedPrompt.UserPrompt.Trim(),
         rawRequestJson,
         null,
         null,
         0,
         rawRequestJson.Length,
         null,
         null,
         DateTimeOffset.UtcNow,
         null,
         null,
         null,
         null,
         null
      );

      async Task ReportToolTraceProgressAsync(
         string? toolTraceJson,
         CancellationToken progressCancellationToken
      )
      {
         run = run with
         {
            ToolTraceJson = toolTraceJson
         };

         try
         {
            await runRepository.UpdateToolTraceAsync(
               run.Id,
               toolTraceJson,
               progressCancellationToken
            );
         }
         catch(Exception)
         {
         }
      }

      await runRepository.StoreAsync(run, cancellationToken);

      try
      {
         var providerResult = await providerClient.GenerateAsync(
            provider,
            job,
            prompt,
            renderedPrompt,
            request.InputPayloadJson,
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
            run.RenderedPrompt,
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
            run.RenderedPrompt,
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
            run.RenderedPrompt,
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
}
