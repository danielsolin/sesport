namespace SESport.AI.WebSearch;

public sealed record WebSearchResponse(
   IReadOnlyList<WebSearchResult> Results,
   string? Provider = null,
   string? Details = null
);
