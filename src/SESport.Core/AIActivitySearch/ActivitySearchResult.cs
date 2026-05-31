using SESport.Core.Ingestion;

namespace SESport.Core.AIActivitySearch;

public sealed record ActivitySearchResult(
   ActivitySearchEntity Entity,
   IReadOnlyCollection<ActivityProposal> Proposals,
   string RawContent,
   string RawResponse
);
