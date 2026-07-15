namespace SESport.Core.Configuration;

public sealed record SearxngWebSearchClientOptions
{
   public string? BaseUrl { get; init; }

   public IReadOnlyList<string>? Engines { get; init; }
}
