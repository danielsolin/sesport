namespace SESport.Core.Ingestion;

public sealed record ActivityProposalGroup(
   ActivityProposalGroupId Id,
   string Fingerprint,
   IReadOnlyCollection<ActivityProposalId> ProposalIds,
   ActivityId? ActivityId
);
