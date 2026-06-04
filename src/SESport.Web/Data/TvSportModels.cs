namespace SESport.Web.Data;

public sealed record TvSportBroadcastListItem(
   Guid Id,
   string TimeText,
   string ChannelName,
   string Title,
   string? Description,
   string Categories,
   bool IsReplay,
   DateOnly? OriginalAirDate
)
{
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

public sealed record TvSportCategoryOption(
   string Name,
   bool IsSelected
);

public sealed record TvSportBroadcastActivitySource(
   Guid Id,
   string ChannelName,
   string Title,
   string? Description,
   IReadOnlyList<string> Categories,
   DateTimeOffset StartsAt,
   DateTimeOffset EndsAt
);
