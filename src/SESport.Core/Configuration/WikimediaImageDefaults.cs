namespace SESport.Core.Configuration;

public static class WikimediaImageDefaults
{
   public static readonly Uri ApiUri = new(
      "https://commons.wikimedia.org/w/api.php"
   );
   public static readonly TimeSpan HttpClientTimeout =
      TimeSpan.FromSeconds(30);
   public const int MainImageWidth = 500;
   public const int ListThumbnailWidth = 72;
   public const int MaximumImageBytes = 250_000;
   public const int MaximumThumbnailBytes = 64_000;
   public const int MaximumRetryAttempts = 4;
   public const int MaximumRetryDelaySeconds = 60;
   public const int RetryBackoffBaseSeconds = 5;
}
