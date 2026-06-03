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
);

public sealed record TvSportCategoryOption(
   string Name,
   bool IsSelected
);
