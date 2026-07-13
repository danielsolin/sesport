using System.Text.Json;

using SESport.AI.Interfaces;
using SESport.AI.Jobs;
using SESport.Core.AI;
using SESport.Data;

namespace SESport.Web.Services;

public sealed class ActivityTeaserJobProcessor(
   AiJobRunner inner,
   IAiJobRunRepository runRepository,
   ActivityRepository activityRepository,
   ILogger<ActivityTeaserJobProcessor> logger
) : IAiJobProcessor
{
   public async Task ProcessRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      await inner.ProcessRunAsync(runId, cancellationToken);
      await SaveCompletedActivityTextAsync(runId, cancellationToken);
   }

   private async Task SaveCompletedActivityTextAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      var run = await runRepository.GetRunAsync(runId, cancellationToken);

      if(run is null ||
         (run.JobId != AiJobIds.GenerateActivityTeaser &&
            run.JobId != AiJobIds.FindActivityFacts) ||
         !string.Equals(
            run.StatusId,
            AiJobRunStatusIds.Completed,
            StringComparison.Ordinal
         ))
      {
         return;
      }

      if(!Guid.TryParse(run.CorrelationId, out var activityId))
      {
         logger.LogWarning(
            "Activity text run {RunId} has invalid correlation id.",
            runId
         );
         return;
      }

      var output = run.JobId == AiJobIds.GenerateActivityTeaser
         ? ExtractGeneratedTeaser(run.OutputText ?? string.Empty)
         : ExtractGeneratedFacts(run.OutputText ?? string.Empty);

      if(string.IsNullOrWhiteSpace(output))
      {
         logger.LogWarning(
            "Activity text run {RunId} completed without output.",
            runId
         );
         return;
      }

      if(run.JobId == AiJobIds.GenerateActivityTeaser)
      {
         await activityRepository.UpdateTeaserAsync(
            activityId,
            output,
            cancellationToken
         );
      }
      else
      {
         await activityRepository.UpdateFactsAsync(
            activityId,
            output,
            cancellationToken
         );
      }
   }

   internal static string? ExtractGeneratedTeaser(string outputText)
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.TryGetProperty("teaser", out var teaser) &&
            teaser.ValueKind == JsonValueKind.String)
         {
            return teaser.GetString();
         }
      }
      catch(JsonException)
      {
      }

      return outputText;
   }

   internal static string? ExtractGeneratedFacts(string outputText)
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.TryGetProperty("facts", out var facts) &&
            facts.ValueKind == JsonValueKind.String)
         {
            return facts.GetString();
         }
      }
      catch(JsonException)
      {
      }

      return outputText;
   }
}
