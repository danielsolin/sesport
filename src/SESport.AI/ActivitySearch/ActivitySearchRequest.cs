namespace SESport.AI.ActivitySearch;

public sealed record ActivitySearchRequest(
   ActivitySearchEntity Entity,
   DateOnly SearchDate,
   int MaxProposals = 5,
   int LookBackDays = 0,
   int LookAheadDays = 30
);
