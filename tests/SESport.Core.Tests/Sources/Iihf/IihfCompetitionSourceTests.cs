using SESport.Sources.Iihf;

namespace SESport.Core.Tests.Sources.Iihf;

public class IihfCompetitionSourceTests
{
   [Fact]
   public void CompetitionSourceKeepsProviderMappingOutOfCoreDomain()
   {
      var source = new IihfCompetitionSource(
         new CompetitionId("competition:iihf-world-championship-2026"),
         "2026/wm",
         new Uri("https://stats.iihf.com/Hydra/969/index.html")
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
         "https://stats.iihf.com/Hydra/969/index.html",
         source.StatsUri.ToString()
      );
   }
}
