using SESport.Core.Broadcast;

namespace SESport.Data.Models;

public sealed record BroadcastListItem(
   Guid Id,
   string TimeText,
   string ChannelName,
   string Title,
   string? Description,
   IReadOnlyList<string> Categories,
   bool IsReplay,
   DateOnly? OriginalAirDate,
   bool IsHidden,
   Guid? OrganizationEntityId,
   string? OrganizationEntityName,
   string? OrganizationSportName,
   Guid? ActivityGroupId,
   string? ActivityGroupTitle,
   string? ActivityGroupDraftTitle,
   string? ActivityGroupSourceKindId,
   Guid? ActivityGroupSourceActivityId,
   string SourceKey
)
{
   public BroadcastParticipationCheck? ParticipationCheck { get; init; }

   public IReadOnlyList<BroadcastParticipationCheck>
      ParticipationChecks
   { get; init; } = [];

   public IReadOnlyList<ActivityGroupParticipant>
      ActivityGroupParticipants
   { get; init; } = [];
};

public sealed record BroadcastCategoryOption(
   string Name,
   bool IsSelected
);

public sealed record BroadcastSaveResult(
   int SavedCount,
   int InsertedCount,
   int UpdatedCount
);

public sealed record BroadcastIgnoreRule(
   string Kind,
   string Value,
   string? SourceKey
);
