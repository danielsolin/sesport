using System.Text.Json;
using SESport.AI.Interfaces;
using SESport.AI.Jobs;
using SESport.Data;

namespace SESport.Web.Services;

public sealed class ActivityTeaserJobProcessor(
   AiJobRunner inner,
   IAiJobRunRepository runRepository,
   ActivityRepository activityRepository,
   ILogger<ActivityTeaserJobProcessor> logger
) : IAiJobProcessor
{
   private const string TeaserJobId = "generate-activity-teaser";

   public async Task ProcessRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      await inner.ProcessRunAsync(runId, cancellationToken);
      await SaveCompletedTeaserAsync(runId, cancellationToken);
   }

   private async Task SaveCompletedTeaserAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      var run = await runRepository.GetRunAsync(runId, cancellationToken);

      if(run is null ||
         !string.Equals(run.JobId, TeaserJobId, StringComparison.Ordinal) ||
         !string.Equals(run.StatusId, "completed", StringComparison.Ordinal))
      {
         return;
      }

      if(!Guid.TryParse(run.CorrelationId, out var activityId))
      {
         logger.LogWarning(
            "Activity teaser run {RunId} has invalid correlation id.",
            runId
         );
         return;
      }

      var teaser = ExtractGeneratedTeaser(run.OutputText ?? string.Empty);

      if(string.IsNullOrWhiteSpace(teaser))
      {
         logger.LogWarning(
            "Activity teaser run {RunId} completed without teaser output.",
            runId
         );
         return;
      }

      await activityRepository.UpdateTeaserAsync(
         activityId,
         teaser,
         cancellationToken
      );
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
}
