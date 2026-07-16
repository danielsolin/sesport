namespace SESport.Core.Configuration;

public sealed record SearxngWebSearchClientOptions
{
   public const string DefaultBaseUrl = "http://127.0.0.1:8088/";

   public static readonly IReadOnlyList<string> DefaultEngines =
   [
      "google",
      "brave",
      "duckduckgo"
   ];

   public string? BaseUrl { get; init; } = DefaultBaseUrl;

   public IReadOnlyList<string> Engines { get; init; } = DefaultEngines;
}
