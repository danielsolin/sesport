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
   public void ResolveSportIdReturnsFootballForEnglishCategory()
   {
      var sportId = BroadcastCategorySportIdResolver.ResolveSportId(
         ["Football"]
      );

      Assert.Equal("football", sportId);
   }

   [Fact]
   public void ResolveSportIdSplitsCombinedCategories()
   {
      var sportId = BroadcastCategorySportIdResolver.ResolveSportId(
         ["Fotboll, Fotbolls-VM 2026, Grupp F"]
      );

      Assert.Equal("football", sportId);
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
