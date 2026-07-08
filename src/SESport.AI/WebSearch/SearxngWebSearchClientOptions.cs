namespace SESport.AI.WebSearch;

public sealed record SearxngWebSearchClientOptions
{
   public string? BaseUrl { get; init; }

   public IReadOnlyList<string>? Engines { get; init; }
}
