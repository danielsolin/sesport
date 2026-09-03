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
            "privacywall",
            "bing",
            "mojeek",
            "braveapi",
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
   }
}
