using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Pages.Admin.Activities;

public sealed class BroadcastActivityTypeResolverTests
{
   [Fact]
   public void ResolveActivityTypeReturnsTournamentForGolf()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "The Masters",
         "Golf from Augusta",
         ["golf"]
      );

      Assert.Equal(ActivityType.Tournament, activityType);
   }

   [Fact]
   public void ResolveActivityTypeReturnsQualificationForQualifier()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Sweden vs Finland",
         "Qualifier for the World Championship",
         ["football"]
      );

      Assert.Equal(ActivityType.Qualification, activityType);
   }

   [Fact]
   public void ResolveActivityTypeReturnsQualificationForTitle()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "World Championship qualification",
         null,
         ["football"]
      );

      Assert.Equal(ActivityType.Qualification, activityType);
   }

   [Fact]
   public void ResolveActivityTypeReturnsPracticeForTitle()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Practice session",
         null,
         ["golf"]
      );

      Assert.Equal(ActivityType.Practice, activityType);
   }

   [Fact]
   public void ResolveActivityTypeReturnsRaceForMotorsport()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Grand Prix",
         null,
         ["Motorsport"]
      );

      Assert.Equal(ActivityType.Race, activityType);
   }

   [Theory]
   [InlineData("Motocross")]
   [InlineData("Rally")]
   [InlineData("Speedway")]
   public void ResolveActivityTypeReturnsRaceForSpecificMotorSports(
      string category
   )
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Swedish round",
         null,
         [category]
      );

      Assert.Equal(ActivityType.Race, activityType);
   }

   [Fact]
   public void ResolveActivityTypeReturnsRaceForCycling()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Tour de France",
         null,
         ["Cycling"]
      );

      Assert.Equal(ActivityType.Race, activityType);
   }

   [Theory]
   [InlineData("Cykel")]
   [InlineData("Mountainbike")]
   public void ResolveActivityTypeReturnsRaceForSwedishCyclingCategories(
      string category
   )
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Cross country Olympic",
         null,
         [category]
      );

      Assert.Equal(ActivityType.Race, activityType);
   }

   [Fact]
   public void ResolveActivityTypeUsesCyclingSportId()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Cross country Olympic",
         null,
         ["Sport"],
         SportIds.Cycling
      );

      Assert.Equal(ActivityType.Race, activityType);
   }

   [Theory]
   [InlineData("Cycling stage", ActivityType.Stage)]
   [InlineData("Cycling World Cup", ActivityType.Championship)]
   public void ResolveActivityTypePrioritizesExplicitCompetitionTypes(
      string title,
      ActivityType expectedActivityType
   )
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         title,
         null,
         ["Cykel"],
         SportIds.Cycling
      );

      Assert.Equal(expectedActivityType, activityType);
   }

   [Fact]
   public void ResolveActivityTypeReturnsMatchForKnownMatchSport()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Sweden vs Finland",
         null,
         [],
         SportIds.Football
      );

      Assert.Equal(ActivityType.Match, activityType);
   }

   [Fact]
   public void ResolveActivityTypeReturnsEventForAthletics()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Diamond League: Final",
         "Athletics from Brussels",
         ["Friidrott"],
         SportIds.Athletics
      );

      Assert.Equal(ActivityType.Event, activityType);
   }

   [Fact]
   public void ResolveActivityTypeReturnsNullForUnknownSport()
   {
      var activityType = BroadcastActivityTypeResolver.ResolveActivityType(
         "Sports programme",
         null,
         [],
         "unknown-sport"
      );

      Assert.Null(activityType);
   }
}
