using SESport.AI.Jobs;
using SESport.Core.AI;

namespace SESport.Web.Workers;

public sealed class AiPendingRunWorker(
   IServiceScopeFactory scopeFactory,
   ILogger<AiPendingRunWorker> logger,
   AiPendingRunWakeSignal pendingRunWakeSignal
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

               await WaitForWorkAsync(
                  activeRuns.Values,
                  stoppingToken
               );
               continue;
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

   private async Task WaitForWorkAsync(
      IEnumerable<Task> activeRuns,
      CancellationToken stoppingToken
   )
   {
      using var waitCancellation =
         CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken
         );
      var signalTask = pendingRunWakeSignal.WaitAsync(
         waitCancellation.Token
      ).AsTask();
      var pollTask = Task.Delay(
         AiWorkerDefaults.PendingRunPollInterval,
         waitCancellation.Token
      );
      var waitTasks = activeRuns
         .Append(signalTask)
         .Append(pollTask)
         .ToArray();

      try
      {
         await Task.WhenAny(waitTasks);
      }
      finally
      {
         waitCancellation.Cancel();
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
