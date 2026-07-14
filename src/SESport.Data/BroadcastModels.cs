using SESport.Core.Broadcast;
using SESport.Core.Domain;

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
   string? ActivityGroupSourceKindId,
   Guid? ActivityGroupSourceActivityId
)
{
   public string CategoriesText => string.Join(", ", Categories);

   public string GroupText
   {
      get
      {
         if(string.IsNullOrWhiteSpace(ActivityGroupSourceKindId))
         {
            return "-";
         }

         if(!string.Equals(
            ActivityGroupSourceKindId,
            BroadcastActivitySourceKindIds.ActivityGroupForActivity,
            StringComparison.Ordinal
         ))
         {
            return ActivityGroupSourceKindId;
         }

         return ActivityGroupSourceActivityId is null
            ? "Will create new ActivityGroup"
            : "Will reuse existing ActivityGroup";
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
