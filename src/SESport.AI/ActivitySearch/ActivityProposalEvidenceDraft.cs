namespace SESport.AI.ActivitySearch;

public sealed record ActivityProposalEvidenceDraft(
   string? SourceName,
   Uri? Uri,
   string? Title,
   string Summary,
   string? RawExcerpt
);
