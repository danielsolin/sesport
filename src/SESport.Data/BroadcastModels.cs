using SESport.Core.Broadcast;

namespace SESport.Data;

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
   Guid? ActivityGroupId,
   string? ActivityGroupTitle,
   string? ActivityGroupDraftTitle,
   string? ActivityGroupSourceKindId,
   Guid? ActivityGroupSourceActivityId
)
{
   public BroadcastParticipationCheck? ParticipationCheck { get; init; }

   public IReadOnlyList<BroadcastParticipationCheck>
      ParticipationChecks
   { get; init; } = [];
};

public sealed record BroadcastCategoryOption(
   string Name,
   bool IsSelected
);
