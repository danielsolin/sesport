namespace SESport.Core.Configuration;

public static class WebSearchDefaults
{
   public const string GoogleSearchBaseUrl =
      "https://www.google.com/search";

   public const int MaxSearchResults = 20;
   public const int RelevanceMinimumQueryTermCount = 4;
   public const int RelevanceMinimumResultCount = 5;
   public const int RelevanceMinimumMatchingTermCount = 2;

   public static readonly IReadOnlyList<string> DeniedHostSuffixes =
   [
      "instagram.com",
      "www.instagram.com",
      "facebook.com",
      "www.facebook.com",
      "x.com",
      "www.x.com",
      "twitter.com",
      "www.twitter.com",
      "tiktok.com",
      "www.tiktok.com",
      "youtube.com",
      "www.youtube.com",
      "youtu.be",
      "www.youtu.be",
      "threads.net",
      "www.threads.net"
   ];
}
