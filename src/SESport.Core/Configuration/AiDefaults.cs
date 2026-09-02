namespace SESport.Core.Configuration;

public static class AiDefaults
{
   public const int DefaultMaxOutputTokens = 8192;

   public const string OpenRouterMetadataHeader =
      "X-OpenRouter-Experimental-Metadata";

   public const string OpenRouterMetadataValue = "enabled";

   public const int OpenRouterMaxRateLimitRetries = 5;

   public static readonly TimeSpan OpenRouterHttpClientTimeout =
      TimeSpan.FromMinutes(90);
   public static readonly TimeSpan LlamaServerHttpClientTimeout =
      TimeSpan.FromMinutes(20);
   public static readonly TimeSpan SearxngHttpClientTimeout =
      TimeSpan.FromSeconds(60);
   public static readonly TimeSpan WebPageContentHttpClientTimeout =
      TimeSpan.FromSeconds(30);

   // Maximum page fetch attempts per URL in one Llama tool loop. Failed
   // attempts are retried until this budget is exhausted; the last
   // failure is then replayed instead of fetching again.
   public const int LlamaPageFetchMaxAttemptsPerUrl = 3;

   public static readonly TimeSpan GoogleTranslatePageTimeout =
      TimeSpan.FromSeconds(60);
   public static readonly TimeSpan GoogleTranslateStabilityDelay =
      TimeSpan.FromMilliseconds(500);
   public static readonly TimeSpan OpenRouterDefaultRetryDelay =
      TimeSpan.FromSeconds(10);
}
