namespace SESport.Core.Ingestion;

public sealed record ActivityProposalEvidence(
   IngestionSource Source,
   Uri? Uri,
   string? Title,
   DateTimeOffset ObservedAt,
   string Summary,
   string? RawExcerpt
);
