namespace SESport.Core.AIActivitySearch;

public sealed record ActivityProposalEvidenceDraft(
   string? SourceName,
   Uri? Uri,
   string? Title,
   string Summary,
   string? RawExcerpt
);
