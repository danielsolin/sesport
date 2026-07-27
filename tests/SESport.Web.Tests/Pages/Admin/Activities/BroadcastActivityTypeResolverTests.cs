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
}
