using SESport.Sources.Iihf;

namespace SESport.Core.Tests.Sources.Iihf;

public class IihfActivityContextSourceTests
{
   private static readonly Uri ExampleIihfStatsUri =
      new("https://example.test/iihf/stats");

   [Fact]
   public void ActivityContextSourceKeepsProviderMappingOutOfCoreDomain()
   {
      var source = new IihfActivityContextSource(
         "2026 IIHF Ice Hockey World Championship",
         "2026/wm",
         ExampleIihfStatsUri
      );

      Assert.Equal(
         "2026 IIHF Ice Hockey World Championship",
         source.ActivityContext
      );

      Assert.Equal(
         "2026/wm",
         source.EventPath
      );

      Assert.Equal(
         ExampleIihfStatsUri.OriginalString,
         source.StatsUri.ToString()
      );
   }
}
