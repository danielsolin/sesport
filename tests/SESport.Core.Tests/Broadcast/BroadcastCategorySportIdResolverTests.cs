using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Broadcast;

public sealed class BroadcastCategorySportIdResolverTests
{
   [Fact]
   public void ResolveSportIdReturnsMotorsportForMotorsportCategory()
   {
      var sportId = BroadcastCategorySportIdResolver.ResolveSportId(
         ["motorsport"]
      );

      Assert.Equal("motorsport", sportId);
   }

   [Fact]
   public void ResolveSportIdReturnsMultiSportForCyclingCategory()
   {
      var sportId = BroadcastCategorySportIdResolver.ResolveSportId(
         ["cycling"]
      );

      Assert.Equal("multi-sport", sportId);
   }
}
