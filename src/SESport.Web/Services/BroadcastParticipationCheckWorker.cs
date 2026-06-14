using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SESport.Web.Services;

public sealed class BroadcastParticipationCheckWorker(
   IServiceScopeFactory scopeFactory,
   BroadcastParticipationCheckQueue queue,
   ILogger<BroadcastParticipationCheckWorker> logger
) : BackgroundService
{
   protected override async Task ExecuteAsync(
      CancellationToken stoppingToken
   )
   {
      await foreach(
         var workItem in queue.DequeueAsync(stoppingToken)
      )
      {
         try
         {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<
               BroadcastParticipationService
            >();

            await service.CheckSwedishParticipationAsync(
               workItem.BroadcastIds,
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
               "Background AI check failed."
            );
         }
      }
   }
}
