using SESport.AI.Jobs;
using SESport.Core.AI;

namespace SESport.Web.Workers;

public sealed class AiPendingRunWorker(
   IServiceScopeFactory scopeFactory,
   ILogger<AiPendingRunWorker> logger
) : BackgroundService
{
   protected override async Task ExecuteAsync(
      CancellationToken stoppingToken
   )
   {
      var activeRuns = new Dictionary<string, Task>(StringComparer.Ordinal);

      try
      {
         while(!stoppingToken.IsCancellationRequested)
         {
            try
            {
               await ObserveCompletedRunsAsync(
                  activeRuns,
                  stoppingToken
               );

               var claim = await ClaimNextRunAsync(
                  activeRuns.Keys.ToArray(),
                  stoppingToken
               );

               if(claim is not null)
               {
                  activeRuns.Add(
                     claim.ProviderId,
                     ProcessRunAsync(claim.RunId, stoppingToken)
                  );
                  continue;
               }

               if(activeRuns.Count > 0)
               {
                  await Task.WhenAny(activeRuns.Values);
                  continue;
               }

               await Task.Delay(
                  AiWorkerDefaults.PendingRunPollInterval,
                  stoppingToken
               );
            }
            catch(OperationCanceledException)
               when(stoppingToken.IsCancellationRequested)
            {
               return;
            }
            catch(Exception exception)
            {
               logger.LogError(
                  exception,
                  "AI run worker failed while dispatching runs."
               );

               await Task.Delay(
                  AiWorkerDefaults.PendingRunPollInterval,
                  stoppingToken
               );
            }
         }
      }
      finally
      {
         await ObserveAllRunsAsync(activeRuns, stoppingToken);
      }
   }

   private async Task<AiJobRunClaim?> ClaimNextRunAsync(
      IReadOnlyCollection<string> busyProviderIds,
      CancellationToken cancellationToken
   )
   {
      using var scope = scopeFactory.CreateScope();
      var runRepository = scope.ServiceProvider.GetRequiredService<
         IAiJobRunRepository
      >();

      return await runRepository.ClaimNextRunAsync(
         busyProviderIds,
         cancellationToken
      );
   }

   private async Task ProcessRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      using var scope = scopeFactory.CreateScope();
      var processor = scope.ServiceProvider.GetRequiredService<
         IAiJobProcessor
      >();

      await processor.ProcessRunAsync(runId, cancellationToken);
   }

   private async Task ObserveCompletedRunsAsync(
      Dictionary<string, Task> activeRuns,
      CancellationToken stoppingToken
   )
   {
      foreach(var activeRun in activeRuns.ToArray())
      {
         if(!activeRun.Value.IsCompleted)
         {
            continue;
         }

         await ObserveRunAsync(activeRun.Value, stoppingToken);
         activeRuns.Remove(activeRun.Key);
      }
   }

   private async Task ObserveAllRunsAsync(
      Dictionary<string, Task> activeRuns,
      CancellationToken stoppingToken
   )
   {
      foreach(var activeRun in activeRuns.Values)
      {
         await ObserveRunAsync(activeRun, stoppingToken);
      }
   }

   private async Task ObserveRunAsync(
      Task runTask,
      CancellationToken stoppingToken
   )
   {
      try
      {
         await runTask;
      }
      catch(OperationCanceledException)
         when(stoppingToken.IsCancellationRequested)
      {
      }
      catch(Exception exception)
      {
         logger.LogError(exception, "AI run processing task failed.");
      }
   }
}
