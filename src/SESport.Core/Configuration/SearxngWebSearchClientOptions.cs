namespace SESport.Core.Configuration;

public sealed record SearxngWebSearchClientOptions
{
   public const string DefaultBaseUrl = "http://127.0.0.1:8088/";

   public static readonly IReadOnlyList<string> DefaultEngines =
   [
      "google",
      "brave",
      "duckduckgo",
      "bing",
      "mojeek",
      "privacywall",
      "seznam",
      "naver",
      "boardreader",
      "yep",
      "yahoo",
      "google_cse",
      "gmx",
      "resulthunter"
   ];

   public static readonly IReadOnlyList<string> DefaultRecentEngines =
   [
      "yahoo",
      "privacywall",
      "duckduckgo",
      "mojeek",
      "resulthunter",
      "gmx"
   ];

   public string? BaseUrl { get; init; } = DefaultBaseUrl;

   public IReadOnlyList<string> Engines { get; init; } = DefaultEngines;

   public IReadOnlyList<string> RecentEngines { get; init; } =
      DefaultRecentEngines;
}
