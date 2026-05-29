using SESport.Sources.Iihf;

namespace SESport.Core.Tests.Sources.Iihf;

public class IihfCompetitionSourceTests
{
   private static readonly Uri ExampleIihfStatsUri =
      new("https://example.test/iihf/stats");

   [Fact]
   public void CompetitionSourceKeepsProviderMappingOutOfCoreDomain()
   {
      var source = new IihfCompetitionSource(
         new CompetitionId("competition:iihf-world-championship-2026"),
         "2026/wm",
         ExampleIihfStatsUri
      );

      Assert.Equal(
         "competition:iihf-world-championship-2026",
         source.CompetitionId.Value
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
