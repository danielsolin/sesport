namespace SESport.Web.Workers;

public sealed class MemberPushNotificationWorker(
   IServiceScopeFactory scopeFactory,
   MemberPushOptions options,
   IHostEnvironment environment,
   ILogger<MemberPushNotificationWorker> logger
) : BackgroundService
{
   protected override async Task ExecuteAsync(
      CancellationToken stoppingToken
   )
   {
      if(!IsWorkerAllowed(options, environment))
      {
         logger.LogInformation(
            "Member push notification worker is disabled outside " +
            "the explicitly enabled production service."
         );
         return;
      }

      if(!options.IsConfigured)
      {
         logger.LogWarning(
            "Member push notifications are disabled because " +
            "MemberPush is not configured."
         );
         return;
      }

      try
      {
         await SweepAsync(stoppingToken);

         using var timer = new PeriodicTimer(
            GetSweepInterval()
         );
         while(await timer.WaitForNextTickAsync(stoppingToken))
         {
            await SweepAsync(stoppingToken);
         }
      }
      catch(OperationCanceledException)
         when(stoppingToken.IsCancellationRequested)
      {
      }
   }

   internal static bool IsWorkerAllowed(
      MemberPushOptions options,
      IHostEnvironment environment
   )
   {
      return environment.IsProduction() && options.WorkerEnabled;
   }

   private async Task SweepAsync(CancellationToken cancellationToken)
   {
      try
      {
         using var scope = scopeFactory.CreateScope();
         var repository = scope.ServiceProvider.GetRequiredService<
            MemberPushRepository
         >();
         var sender = scope.ServiceProvider.GetRequiredService<
            MemberPushNotificationSender
         >();
         var now = DateTimeOffset.UtcNow;
         var notifications = await repository.ClaimDueNotificationsAsync(
            now,
            now.Subtract(GetClaimLease()),
            options.DefaultNotificationLeadTimeMinutes,
            options.NotificationBatchSize,
            cancellationToken
         );

         foreach(var notification in notifications)
         {
            var result = await sender.SendAsync(
               notification,
               now,
               cancellationToken
            );

            if(result.HasDelivery || !result.HasTransientFailure)
            {
               await repository.MarkNotificationSentAsync(
                  notification.MemberId,
                  notification.ActivityId,
                  now,
                  cancellationToken
               );
            }
         }
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
      }
      catch(Exception exception)
      {
         logger.LogError(
            exception,
            "Member push notification sweep failed."
         );
      }
   }

   private TimeSpan GetSweepInterval()
   {
      return TimeSpan.FromSeconds(
         Math.Max(
            MemberPushOptions.MinimumSweepIntervalSeconds,
            options.NotificationSweepIntervalSeconds
         )
      );
   }

   private TimeSpan GetClaimLease()
   {
      return options.NotificationClaimLease > TimeSpan.Zero
         ? options.NotificationClaimLease
         : TimeSpan.FromMinutes(MemberPushOptions.DefaultClaimLeaseMinutes);
   }
}
