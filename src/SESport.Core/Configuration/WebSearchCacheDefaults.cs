namespace SESport.Core.Configuration;

public static class WebSearchCacheDefaults
{
   public const int MaximumEntryCount = 1000;

   public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);
}
