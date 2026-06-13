namespace SESport.AI.Providers;

public sealed record WebSearchResult(
   string Title,
   string Url,
   string? Snippet,
   DateTimeOffset? PublishedAt = null
);
