namespace SESport.Core.AIActivitySearch;

public sealed record ActivitySearchRequest(
   ActivitySearchEntity Entity,
   DateOnly SearchDate,
   int MaxProposals = 5,
   bool AllowWebSearch = true,
   int LookBackDays = 0,
   int LookAheadDays = 30
);
