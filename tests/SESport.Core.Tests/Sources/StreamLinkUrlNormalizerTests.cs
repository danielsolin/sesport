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
            "c1a3215a-6586-5efb-a746-16df214c1c23",
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

   [Fact]
   public void TryNormalizeRemovesTrackingQueryParameters()
   {
      var normalized = StreamLinkUrlNormalizer.TryNormalize(
         "https://play.example/video?id=123&utm_source=search.example&" +
         "tag=campaign0f-21",
         out var normalizedUrl
      );

      Assert.True(normalized);
      Assert.Equal("https://play.example/video?id=123", normalizedUrl);
   }

   [Fact]
   public void TryNormalizeExtractsUrlQueryParameter()
   {
      var sourceUrl =
         "https://track.adtraction.com/t/t?a=123&as=456&url=" +
         "https://play.example/video?id=123";

      var normalized = StreamLinkUrlNormalizer.TryNormalize(
         sourceUrl,
         out var normalizedUrl
      );

      Assert.True(normalized);
      Assert.Equal(
         "https://play.example/video?id=123",
         normalizedUrl
      );
   }

   [Fact]
   public void TryNormalizeExtractsShortUrlQueryParameter()
   {
      var sourceUrl =
         "https://affiliate.example/click?u=" +
         "https://play.example/video/123";

      var normalized = StreamLinkUrlNormalizer.TryNormalize(
         sourceUrl,
         out var normalizedUrl
      );

      Assert.True(normalized);
      Assert.Equal(
         "https://play.example/video/123",
         normalizedUrl
      );
   }
}
