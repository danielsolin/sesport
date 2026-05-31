namespace SESport.Core.Ingestion;

public sealed record ActivityProposalEvidence(
   Source Source,
   Uri? Uri,
   string? Title,
   DateTimeOffset ObservedAt,
   string Summary,
   string? RawExcerpt
);
