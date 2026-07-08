namespace SESport.AI.WebSearch;

public sealed record WebSearchResult(
   string Title,
   string Url,
   string? Snippet,
   DateTimeOffset? PublishedAt = null
);
