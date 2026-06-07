namespace SESport.AI.ActivitySearch;

public sealed record ActivityProposalDraft(
   string Title,
   string? Description,
   string ActivityType,
   DateOnly ActivityDate,
   TimeOnly? LocalStartTime,
   string TimeZoneId,
   string? Context,
   string EntityRole,
   string EntityExplanation,
   decimal? Confidence,
   IReadOnlyCollection<ActivityProposalEvidenceDraft> Evidence
);
