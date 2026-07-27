namespace SESport.Core.Broadcast;

public sealed record BroadcastActivitySource(
   Guid Id,
   string ChannelName,
   string Title,
   string? Description,
   IReadOnlyList<string> Categories,
   DateTimeOffset StartsAt,
   DateTimeOffset EndsAt,
   Guid? EntityId = null,
   string? ActivityGroupSourceKindId = null,
   Guid? ActivityGroupSourceActivityId = null,
   string? ActivityGroupDraftTitle = null,
   string? EntitySportId = null
);
