using System.Text.Json;
using SESport.Core.AI.Abstractions;
using SESport.Core.AI.Models;

namespace SESport.Core.AI;

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
            $"AI job '{request.JobId}' is not configured."
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
            $"AI provider '{job.ProviderId}' is not configured."
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
      var run = new AiJobRun(
         Guid.NewGuid(),
         job.Id,
         prompt.Id,
         provider.Id,
         AiJobRunStatus.Running,
         request.CorrelationId,
         request.InputPayloadJson,
         renderedPrompt,
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

      await runRepository.StoreAsync(run, cancellationToken);

      try
      {
         var providerResult = await providerClient.GenerateAsync(
            provider,
            job,
            prompt,
            renderedPrompt,
            request.InputPayloadJson,
            cancellationToken
         );

         run = run with
         {
            Status = AiJobRunStatus.Completed,
            RawResponseJson = providerResult.RawResponseJson,
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
            run.RenderedPrompt,
            run.OutputText ?? string.Empty,
            run.RawResponseJson,
            null
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
            run.RenderedPrompt,
            run.OutputText ?? string.Empty,
            run.RawResponseJson,
            exception.Message
         );
      }
   }
}
