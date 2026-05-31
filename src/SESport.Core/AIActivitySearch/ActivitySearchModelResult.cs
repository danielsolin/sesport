namespace SESport.Core.AIActivitySearch;

public sealed record ActivitySearchModelResult(
   string RawContent,
   string RawResponse,
   IReadOnlyCollection<ActivityProposalDraft> Proposals
);
