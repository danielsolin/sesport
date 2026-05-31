namespace SESport.Core.Domain;

public sealed record ActivityEvidence(
   Source Source,
   Uri? Uri,
   string? Title,
   DateTimeOffset ObservedAt,
   string Summary,
   ActivityProposalId? ProposalId
);
