namespace SESport.Data.Models;

public sealed record MemberPushSubscription(
   Guid Id,
   string Endpoint,
   string P256dh,
   string Auth
);

public sealed record MemberPushSubscriptionInput(
   string Endpoint,
   string P256dh,
   string Auth,
   DateTimeOffset? ExpirationAt
);

public sealed record MemberActivityPushNotification(
   Guid MemberId,
   Guid ActivityId,
   string ActivityTitle,
   string PersonNames,
   DateTimeOffset StartsAt,
   string PublicDateMode,
   int LeadTimeMinutes,
   string? TvChannelName,
   IReadOnlyList<MemberPushSubscription> Subscriptions
);
