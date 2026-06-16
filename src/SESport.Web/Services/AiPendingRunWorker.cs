using SESport.AI.Abstractions;
using SESport.AI.Services;

namespace SESport.Web.Services;

public sealed class AiPendingRunWorker(
   IServiceScopeFactory scopeFactory,
   AiJobExecutionGate executionGate,
   ILogger<AiPendingRunWorker> logger
) : BackgroundService
{
   private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      while(!stoppingToken.IsCancellationRequested)
      {
         Guid? runId = null;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var runRepository = scope.ServiceProvider.GetRequiredService<
               IAiJobRunRepository
            >();
            var processor = scope.ServiceProvider.GetRequiredService<
               IAiJobProcessor
            >();

            await executionGate.WaitAsync(stoppingToken);
            try
            {
               runId = await runRepository.ClaimNextRunAsync(
                  stoppingToken
               );

               if(runId is not null)
               {
                  await processor.ProcessRunAsync(
                     runId.Value,
                     stoppingToken
                  );
               }
            }
            finally
            {
               executionGate.Release();
            }
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
               "AI run worker failed."
            );
         }

         if(runId is null)
         {
            try
            {
               await Task.Delay(PollInterval, stoppingToken);
            }
            catch(OperationCanceledException)
               when(stoppingToken.IsCancellationRequested)
            {
               return;
            }
         }
      }
   }
}
