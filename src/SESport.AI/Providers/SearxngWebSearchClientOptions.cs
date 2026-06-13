namespace SESport.AI.Providers;

public sealed record SearxngWebSearchClientOptions
{
   public string? BasicAuthUsername { get; init; }

   public string? BasicAuthPassword { get; init; }
}
