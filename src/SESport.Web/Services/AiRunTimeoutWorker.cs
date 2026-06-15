using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SESport.AI.Abstractions;

namespace SESport.Web.Services;

public sealed class AiRunTimeoutWorker(
   IServiceScopeFactory scopeFactory,
   ILogger<AiRunTimeoutWorker> logger
) : BackgroundService
{
   private static readonly TimeSpan StaleRunAge = TimeSpan.FromHours(1);
   private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(10);

   protected override async Task ExecuteAsync(
      CancellationToken stoppingToken
   )
   {
      await SweepAsync(stoppingToken);

      using var timer = new PeriodicTimer(SweepInterval);

      while(await timer.WaitForNextTickAsync(stoppingToken))
      {
         await SweepAsync(stoppingToken);
      }
   }

   private async Task SweepAsync(CancellationToken cancellationToken)
   {
      try
      {
         using var scope = scopeFactory.CreateScope();
         var repository = scope.ServiceProvider.GetRequiredService<
            IAiJobRunRepository
         >();

         var updatedCount = await repository.FailStaleRunningRunsAsync(
            StaleRunAge,
            cancellationToken
         );

         if(updatedCount > 0)
         {
            logger.LogWarning(
               "Marked {Count} stale AI runs as failed.",
               updatedCount
            );
         }
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
      }
      catch(Exception exception)
      {
         logger.LogError(exception, "AI run timeout sweep failed.");
      }
   }
}
