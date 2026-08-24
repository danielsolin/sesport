using SESport.Core.Sources;

namespace SESport.Core.Tests.Sources;

public sealed class StreamLinkUrlNormalizerTests
{
   [Fact]
   public void TryNormalizeExtractsAffiliateDestination()
   {
      var sourceUrl =
         "https://max.prf.hn/click/camref:1011l3PaYv/" +
         "pubref:Tennis%3A+US+Open/" +
         "destination:https://play.hbomax.com/video/watch/" +
         "c1a3215a-6586-5efb-a746-16df214c1c23?" +
         "utm_source=universal_search";

      var normalized = StreamLinkUrlNormalizer.TryNormalize(
         sourceUrl,
         out var normalizedUrl
      );

      Assert.True(normalized);
      Assert.Equal(
         "https://play.hbomax.com/video/watch/" +
            "c1a3215a-6586-5efb-a746-16df214c1c23?" +
            "utm_source=universal_search",
         normalizedUrl
      );
   }

   [Fact]
   public void TryNormalizeKeepsRegularStreamUrl()
   {
      var normalized = StreamLinkUrlNormalizer.TryNormalize(
         "https://play.example/video?id=123",
         out var normalizedUrl
      );

      Assert.True(normalized);
      Assert.Equal("https://play.example/video?id=123", normalizedUrl);
   }
}
