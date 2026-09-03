using SESport.Web.Routing;

namespace SESport.Core.Tests.Routing;

public sealed class PublicSportRoutesTests
{
   [Theory]
   [InlineData("cycling", "/cykel", "Cykel")]
   [InlineData("ice-hockey", "/ishockey", "Ishockey")]
   [InlineData("football", "/fotboll", "Fotboll")]
   public void FindsSwedishRouteForSport(
      string sportId,
      string expectedPath,
      string expectedDisplayName
   )
   {
      var route = PublicSportRoutes.FindBySportId(sportId);

      Assert.NotNull(route);
      Assert.Equal(expectedPath, route.Path);
      Assert.Equal(expectedDisplayName, route.DisplayName);
      Assert.Equal(route, PublicSportRoutes.FindByPath(expectedPath));
   }

   [Fact]
   public void AllRoutesHaveUniqueIdsAndPaths()
   {
      Assert.Equal(
         PublicSportRoutes.All.Count,
         PublicSportRoutes.All.Select(route => route.SportId).Distinct(
            StringComparer.OrdinalIgnoreCase
         ).Count()
      );
      Assert.Equal(
         PublicSportRoutes.All.Count,
         PublicSportRoutes.All.Select(route => route.Path).Distinct(
            StringComparer.OrdinalIgnoreCase
         ).Count()
      );
   }

   [Fact]
   public void BuildsAbsoluteUrlsFromConfiguredHomeUrl()
   {
      var url = PublicRoutePaths.BuildAbsoluteUrl(
         "https://example.test",
         "/cykel"
      );

      Assert.Equal("https://example.test/cykel", url);
   }
}
