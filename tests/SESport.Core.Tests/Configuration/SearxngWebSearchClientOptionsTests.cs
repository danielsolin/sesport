using SESport.Core.Configuration;

namespace SESport.Core.Tests.Configuration;

public sealed class SearxngWebSearchClientOptionsTests
{
   [Fact]
   public void DefaultsMatchTheCentralSearchConfiguration()
   {
      var options = new SearxngWebSearchClientOptions();

      Assert.Equal(
         SearxngWebSearchClientOptions.DefaultBaseUrl,
         options.BaseUrl
      );
      Assert.Equal(
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
         ],
         options.Engines
      );
      Assert.Equal(
         [
            "yahoo",
            "privacywall",
            "duckduckgo",
            "mojeek",
            "resulthunter",
            "gmx"
         ],
         options.RecentEngines
      );
   }
}
