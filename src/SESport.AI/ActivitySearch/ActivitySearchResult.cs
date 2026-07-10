using SESport.Core.AI;

namespace SESport.AI.ActivitySearch;

public sealed record ActivitySearchResult(
   ActivitySearchEntity Entity,
   IReadOnlyCollection<ActivityProposal> Proposals,
   string RawContent,
   string RawResponse
);
