namespace SESport.Core.Domain;

public sealed record ActivityEvidence(
   IngestionSource Source,
   Uri? Uri,
   string? Title,
   DateTimeOffset ObservedAt,
   string Summary,
   ActivityProposalId? ProposalId
);
