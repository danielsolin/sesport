namespace SESport.Core.Configuration;

public static class AiWorkerDefaults
{
   public static readonly TimeSpan PendingRunPollInterval =
      TimeSpan.FromSeconds(5);
   public static readonly TimeSpan RunTimeoutStaleAge =
      TimeSpan.FromHours(1);
   public static readonly TimeSpan RunTimeoutSweepInterval =
      TimeSpan.FromMinutes(10);

   public const int ActivityAiResultCatchUpMaxRuns = 50;
}
