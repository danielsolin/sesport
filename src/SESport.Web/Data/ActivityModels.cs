namespace SESport.Web.Data;

public sealed record ActivityListItem(
   Guid Id,
   string Title,
   string? Description,
   string ActivityType,
   string SportName,
   string? Context,
   string TimeText,
   string CountryRelevanceExplanation,
   string PublicationStatus,
   string? Slug,
   string EntitySummary
);

public sealed record EntityOption(Guid Id, string Name, string Type);

public sealed class ActivityEditModel
{
   public Guid? Id { get; set; }

   public string Title { get; set; } = string.Empty;

   public string? Description { get; set; }

   public string ActivityType { get; set; } = "Match";

   public string SportId { get; set; } = "football";

   public string SportName { get; set; } = "Football";

   public string? Context { get; set; }

   public string TimeKind { get; set; } = "ExactStart";

   public string? StartsAtLocal { get; set; }

   public DateOnly? StartsOn { get; set; }

   public DateOnly? EndsOn { get; set; }

   public string? TimeDescription { get; set; }

   public string CountryRelevanceExplanation { get; set; } = string.Empty;

   public bool IsPublished { get; set; }

   public string? Slug { get; set; }

   public Guid? EntityId { get; set; }

   public string EntityRole { get; set; } = "CompetesIn";

   public string? EntityExplanation { get; set; }

   public string? EntityContextName { get; set; }

   public string? EvidenceSourceName { get; set; }

   public string? EvidenceUri { get; set; }

   public string? EvidenceTitle { get; set; }

   public string? EvidenceSummary { get; set; }
}
