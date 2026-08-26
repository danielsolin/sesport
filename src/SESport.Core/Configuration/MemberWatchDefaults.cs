namespace SESport.Core.Configuration;

public static class MemberWatchDefaults
{
   public static readonly TimeSpan ImageCacheDuration =
      TimeSpan.FromHours(1);

   public const int MaxSearchResults = 20;

   public const int MinimumSearchLength = 2;
}
