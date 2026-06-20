namespace SESport.AI.Providers;

public sealed record WebSearchResponse(
   IReadOnlyList<WebSearchResult> Results,
   string? Provider = null
);
