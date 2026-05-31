namespace SESport.Core.Ingestion;

public sealed record ActivityProposalEntityLink(
   EntityId EntityId,
   ActivityEntityRole ProposedRole,
   string Explanation,
   string? ContextName,
   decimal? Confidence
);
