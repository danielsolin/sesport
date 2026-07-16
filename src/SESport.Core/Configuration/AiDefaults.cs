namespace SESport.Core.Configuration;

public static class AiDefaults
{
   public const int DefaultMaxOutputTokens = 4096;
   public const int MalformedResponseAttempts = 3;
   public const double ActivitySearchTemperature = 0.1;
   public const string GoogleApiKeyHeader = "x-goog-api-key";

   public const string OpenRouterMetadataHeader =
      "X-OpenRouter-Experimental-Metadata";

   public const string OpenRouterMetadataValue = "enabled";

   public static readonly TimeSpan OpenRouterHttpClientTimeout =
      TimeSpan.FromSeconds(300);
   public static readonly TimeSpan LlamaServerHttpClientTimeout =
      TimeSpan.FromMinutes(20);
   public static readonly TimeSpan SearxngHttpClientTimeout =
      TimeSpan.FromSeconds(60);
   public static readonly TimeSpan GoogleWebSearchHttpClientTimeout =
      TimeSpan.FromSeconds(30);
   public static readonly TimeSpan WebPageContentHttpClientTimeout =
      TimeSpan.FromSeconds(30);
}
