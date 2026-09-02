namespace SESport.Core.Configuration;

public static class WebPageCacheDefaults
{
   public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);
   public const int MaximumEntryCount = 100;
   public const int MaximumCacheableTextCharacters = 250000;
}
