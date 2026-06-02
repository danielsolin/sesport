namespace SESport.Core.Ingestion;

public sealed record ActivityProposal(
   ActivityProposalId Id,
   ActivityProposalProducerType ProducerType,
   Source Source,
   ExternalEntityId? ExternalId,
   string Fingerprint,
   string Title,
   string? Description,
   string? RawContent,
   ActivityType Type,
   ImportedSport Sport,
   string? Context,
   ActivityTime Time,
   IReadOnlyCollection<ActivityProposalEntityLink> EntityLinks,
   IReadOnlyCollection<ActivityProposalEvidence> Evidence,
   decimal? Confidence,
   ActivityProposalStatus Status,
   ActivityProposalRejectReason? RejectReason,
   string? RejectComment,
   ActivityProposalGroupId? GroupId,
   ActivityId? ActivityId,
   string? Producer = null,
   string? Prompt = null
);
