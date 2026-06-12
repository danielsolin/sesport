using SESport.Core.Broadcast;
using SESport.Core.Domain;

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
}
