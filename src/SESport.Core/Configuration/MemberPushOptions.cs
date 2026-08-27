namespace SESport.Core.Configuration;

public sealed record MemberPushOptions
{
   public string Subject { get; init; } = string.Empty;

   public string PublicKey { get; init; } = string.Empty;

   public string PrivateKey { get; init; } = string.Empty;

   public bool WorkerEnabled { get; init; } = false;

   public int DefaultNotificationLeadTimeMinutes { get; init; } =
      MemberNotificationLeadTimes.NoNotificationsMinutes;

   public const int DefaultMaxVisiblePersonNames = 3;

   public int MaxVisiblePersonNames { get; init; } =
      DefaultMaxVisiblePersonNames;

   public const int MinimumSweepIntervalSeconds = 5;

   public const int DefaultClaimLeaseMinutes = 5;

   public int NotificationSweepIntervalSeconds { get; init; } = 30;

   public int NotificationBatchSize { get; init; } = 50;

   public TimeSpan NotificationClaimLease { get; init; } =
      TimeSpan.FromMinutes(DefaultClaimLeaseMinutes);

   public bool IsConfigured =>
      !string.IsNullOrWhiteSpace(Subject) &&
      !string.IsNullOrWhiteSpace(PublicKey) &&
      !string.IsNullOrWhiteSpace(PrivateKey);
}
