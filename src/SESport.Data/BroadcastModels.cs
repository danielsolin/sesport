using SESport.Core.Broadcast;

namespace SESport.Data;

public sealed record BroadcastListItem(
   Guid Id,
   string TimeText,
   string ChannelName,
   string Title,
   string? Description,
   string Categories,
   bool IsReplay,
   DateOnly? OriginalAirDate,
   bool IsHidden
)
{
   public BroadcastParticipationCheck? ParticipationCheck { get; init; }

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
