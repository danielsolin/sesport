namespace SESport.Core.Configuration;

public sealed record MemberPushOptions
{
   public string Subject { get; init; } = string.Empty;

   public string PublicKey { get; init; } = string.Empty;

   public string PrivateKey { get; init; } = string.Empty;

   public bool WorkerEnabled { get; init; } = false;

   public int DefaultNotificationLeadTimeMinutes { get; init; } =
      MemberNotificationLeadTimes.TenMinutes;

   public int NotificationSweepIntervalSeconds { get; init; } = 30;

   public int NotificationBatchSize { get; init; } = 50;

   public TimeSpan NotificationClaimLease { get; init; } =
      TimeSpan.FromMinutes(5);

   public bool IsConfigured =>
      !string.IsNullOrWhiteSpace(Subject) &&
      !string.IsNullOrWhiteSpace(PublicKey) &&
      !string.IsNullOrWhiteSpace(PrivateKey);
}
