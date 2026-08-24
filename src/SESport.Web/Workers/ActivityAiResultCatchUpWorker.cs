using SESport.Core.AI;

namespace SESport.Web.Workers;

public sealed class ActivityAiResultCatchUpWorker(
   IServiceScopeFactory scopeFactory,
   ILogger<ActivityAiResultCatchUpWorker> logger
) : BackgroundService
{
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      try
      {
         using var scope = scopeFactory.CreateScope();
         var applicationRepository = scope.ServiceProvider
            .GetRequiredService<AiRunApplicationRepository>();
         var runRepository = scope.ServiceProvider
            .GetRequiredService<AiJobRunRepository>();
         var activityRepository = scope.ServiceProvider
            .GetRequiredService<ActivityRepository>();
         var factRepository = scope.ServiceProvider
            .GetRequiredService<FactRepository>();
         var participantResultService = scope.ServiceProvider
            .GetRequiredService<ActivityParticipantAiResultService>();

         var runs = await applicationRepository
               .GetCompletedActivityTeaserRunsWithEmptyActivityTeasersAsync(
               AiWorkerDefaults.ActivityAiResultCatchUpMaxRuns,
               stoppingToken
            );

         var factsRuns = await applicationRepository
               .GetUnappliedCompletedActivityGroupFactsRunsAsync(
               AiWorkerDefaults.ActivityAiResultCatchUpMaxRuns,
               stoppingToken
            );
         var participantRuns = await applicationRepository
               .GetUnappliedCompletedActivityParticipantResultRunIdsAsync(
               AiWorkerDefaults.ActivityAiResultCatchUpMaxRuns,
               stoppingToken
            );

         foreach(var run in runs)
         {
            var teaser = AiJobPostProcessor.ExtractGeneratedTeaser(
               run.OutputText
            );

            if(string.IsNullOrWhiteSpace(teaser))
            {
               continue;
            }

            var updated = await activityRepository.UpdateEmptyTeaserAsync(
               run.ActivityId,
               teaser,
               stoppingToken
            );

            if(updated)
            {
               logger.LogInformation(
                  "Saved missed activity teaser from AI run {RunId}.",
                  run.RunId
               );
            }
         }

         foreach(var run in factsRuns)
         {
            var facts = AiJobPostProcessor.ExtractGeneratedActivityFacts(
               run.OutputText
            );

            if(facts is null)
            {
               continue;
            }

            var createdFacts = await factRepository.AddForActivityGroupAsync(
               run.ActivityGroupId,
               facts.Facts,
               stoppingToken
            );

            if(createdFacts.Count > 0)
            {
               await runRepository.RecordApplicationAsync(
                  run.RunId,
                  AiJobRunApplicationTargetTypes.ActivityGroup,
                  run.ActivityGroupId.ToString(),
                  stoppingToken
               );
               logger.LogInformation(
                  "Saved missed activity-group facts from AI run {RunId}.",
                  run.RunId
               );
            }
         }

         foreach(var runId in participantRuns)
         {
            if(await participantResultService.TryApplyRunAsync(
               runId,
               stoppingToken
            ))
            {
               logger.LogInformation(
                  "Saved missed participant AI result from run {RunId}.",
                  runId
               );
            }
         }
      }
      catch(OperationCanceledException)
         when(stoppingToken.IsCancellationRequested)
      {
      }
      catch(Exception exception)
      {
         logger.LogError(
            exception,
            "Activity AI result catch-up failed."
         );
      }
   }
}
