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
   string? ActivityGroupSourceKindId,
   Guid? ActivityGroupSourceActivityId
)
{
   public string CategoriesText => string.Join(", ", Categories);

   public string GroupValue => ActivityGroupTitle ?? Title;

   public string GroupText
   {
      get
      {
         if(!string.Equals(
            ActivityGroupSourceKindId,
            BroadcastActivitySourceKindIds.ActivityGroupForActivity,
            StringComparison.Ordinal
         ))
         {
            return "-";
         }

         return ActivityGroupId is null
            ? $"NEW: {GroupValue}"
            : GroupValue;
      }
   }

   public BroadcastParticipationCheck? ParticipationCheck { get; init; }

   public IReadOnlyList<BroadcastParticipationCheck>
      ParticipationChecks
   { get; init; } = [];

   public string TimeOnlyText
   {
      get
      {
         if(TimeText.Contains(" "))
            return TimeText.Split(' ')[1];

         return TimeText;
      }
   }
};

public sealed record BroadcastCategoryOption(
   string Name,
   bool IsSelected
);
