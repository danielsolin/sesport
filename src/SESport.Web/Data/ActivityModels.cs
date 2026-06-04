namespace SESport.Web.Data;

public sealed record ActivityListItem(
   Guid Id,
   string Title,
   string? Description,
   string ActivityType,
   string SportName,
   string TimeText,
   string PublicationStatus,
   string EntitySummary
);

public sealed record EntityOption(Guid Id, string Name, string Type);

public sealed record LookupOption(string Id, string Label);

public sealed class ActivityEditModel
{
   public Guid? Id { get; set; }

   public string Title { get; set; } = string.Empty;

   public string? Description { get; set; }

   public string ActivityType { get; set; } = "Match";

   public string SportId { get; set; } = "football";

   public DateOnly? ActivityDate { get; set; }

   public TimeOnly? LocalStartTime { get; set; }

   public string TimeZoneId { get; set; } = "Europe/Stockholm";

   public bool IsPublished { get; set; }

   public Guid? EntityId { get; set; }

   public string? EvidenceUri { get; set; }

   public string? EvidenceTitle { get; set; }

   public string? EvidenceComment { get; set; }

   public List<Guid> TvSportBroadcastIds { get; set; } = [];
}
