using SESport.Data;
using SESport.Data.AI;

namespace SESport.Web.Services;

public sealed class ActivityTeaserCatchUpWorker(
   IServiceScopeFactory scopeFactory,
   ILogger<ActivityTeaserCatchUpWorker> logger
) : BackgroundService
{
   private const int MaxRuns = 50;

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      try
      {
         using var scope = scopeFactory.CreateScope();
         var aiRepository = scope.ServiceProvider
            .GetRequiredService<AiRepository>();
         var activityRepository = scope.ServiceProvider
            .GetRequiredService<ActivityRepository>();

         var runs = await aiRepository
            .GetCompletedActivityTeaserRunsWithEmptyActivityTeasersAsync(
               MaxRuns,
               stoppingToken
            );

         foreach(var run in runs)
         {
            var teaser = ActivityTeaserJobProcessor.ExtractGeneratedTeaser(
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
