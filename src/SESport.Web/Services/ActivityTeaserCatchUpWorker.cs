using SESport.Data;
using SESport.Data.AI;
using SESport.Core.AI;
using SESport.Core.Configuration;

namespace SESport.Web.Services;

public sealed class ActivityTeaserCatchUpWorker(
   IServiceScopeFactory scopeFactory,
   ILogger<ActivityTeaserCatchUpWorker> logger
) : BackgroundService
{
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      try
      {
         using var scope = scopeFactory.CreateScope();
         var aiRepository = scope.ServiceProvider
            .GetRequiredService<AiRepository>();
         var activityRepository = scope.ServiceProvider
            .GetRequiredService<ActivityRepository>();
         var factRepository = scope.ServiceProvider
            .GetRequiredService<FactRepository>();

         var runs = await aiRepository
               .GetCompletedActivityTeaserRunsWithEmptyActivityTeasersAsync(
               AiWorkerDefaults.ActivityTeaserCatchUpMaxRuns,
               stoppingToken
            );

         var factsRuns = await aiRepository
               .GetUnappliedCompletedActivityFactsRunsAsync(
               AiWorkerDefaults.ActivityTeaserCatchUpMaxRuns,
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

            var createdFacts = await factRepository.ReplaceForActivityAsync(
               run.ActivityId,
               facts.Facts,
               stoppingToken
            );

            if(createdFacts.Count > 0)
            {
               await aiRepository.RecordApplicationAsync(
                  run.RunId,
                  AiJobRunApplicationTargetTypes.Activity,
                  run.ActivityId.ToString(),
                  stoppingToken
               );
               logger.LogInformation(
                  "Saved missed activity facts from AI run {RunId}.",
                  run.RunId
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
            "Activity teaser catch-up failed."
         );
      }
   }
}
