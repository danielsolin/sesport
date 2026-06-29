using SESport.Core.Domain;

namespace SESport.Data;

public sealed record ActivityListItem(
   Guid Id,
   string Title,
   string? Description,
   string? Teaser,
   string ActivityType,
   string SportId,
   string SportName,
   string? SportIconPath,
   string TimeText,
   DateTimeOffset? StartsAt,
   string? TvChannelName,
   string PublicationStatus,
   string RelatedPersonEntities,
   Guid[] RelatedPersonEntityIds,
   string RelatedOrganizationEntities
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

public sealed record EntityOption(
   Guid Id,
   string Name,
   string Type,
   string Sport,
   string Organization,
   int WatchPrioritySortOrder,
   string? PersonGenderId,
   string? AliasName = null
);

public sealed record LookupOption(string Id, string Label);

public sealed class ActivityEditModel
{
   public Guid? Id { get; set; }

   public string Title { get; set; } = string.Empty;

   public string? Description { get; set; }

   public string? Teaser { get; set; }

   public string ActivityType { get; set; } = string.Empty;

   public string SportId { get; set; } = string.Empty;

   public DateOnly? ActivityDate { get; set; }

   public TimeOnly? LocalStartTime { get; set; }

   public string TimeZoneId { get; set; } = SportDay.TimeZoneId;

   public bool IsPublished { get; set; }

   public List<Guid> LinkedEntityIds { get; set; } = [];

   public string? EvidenceUri { get; set; }

   public string? EvidenceTitle { get; set; }

   public string? EvidenceComment { get; set; }

   public List<Guid> BroadcastIds { get; set; } = [];

   public string? TvChannelName { get; set; }
}
