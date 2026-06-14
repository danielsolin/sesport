using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.AI.Providers;

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
      var renderedPromptText = renderedPrompt.ToPromptText();
      var requestPayload = providerClient.CreateRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt
      );
      var rawRequestJson = AiRequestJsonSerializer.Serialize(requestPayload);
      var displayedRenderedPrompt = BuildRenderedPromptText(
         provider.Kind,
         job.RequiresWebSearch,
         job.ToolsDescription,
         renderedPromptText
      );

      var run = new AiJobRun(
         Guid.NewGuid(),
         job.Id,
         prompt.Id,
         provider.Id,
         provider.Model,
         AiJobRunStatus.Running,
         request.CorrelationId,
         request.InputPayloadJson,
         displayedRenderedPrompt,
         rawRequestJson,
         null,
         null,
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

         run = run with
         {
            Status = AiJobRunStatus.Completed,
            RawRequestJson = providerResult.RawRequestJson,
            RawResponseJson = providerResult.RawResponseJson,
            ToolTraceJson = providerResult.ToolTraceJson,
            OutputText = providerResult.OutputText,
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
            null
         );
      }
      catch(AiProviderExecutionException exception)
      {
         run = run with
         {
            Status = AiJobRunStatus.Failed,
            RawRequestJson = exception.RawRequestJson ?? run.RawRequestJson,
            RawResponseJson =
               exception.RawResponseJson ?? run.RawResponseJson,
            ToolTraceJson = exception.ToolTraceJson ?? run.ToolTraceJson,
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
            exception.Message
         );
      }
   }

   private static string BuildRenderedPromptText(
      string providerKind,
      bool requiresWebSearch,
      string? toolsDescription,
      string renderedPromptText
   )
   {
      if(!requiresWebSearch ||
         !string.Equals(providerKind, "llama-server",
            StringComparison.Ordinal))
      {
         return renderedPromptText;
      }

      return string.Join(
         Environment.NewLine + Environment.NewLine,
         new[]
         {
            renderedPromptText.Trim(),
            toolsDescription?.Trim()
         }.Where(value => !string.IsNullOrWhiteSpace(value))
      );
   }
}
