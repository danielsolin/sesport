using System.Reflection;

using SESport.Data;
using SESport.Web.Services;

namespace SESport.Core.Tests.Services;

public sealed class ActivityIndexPageServiceTests
{
   [Fact]
   public void SortActivitiesOrdersTimeByStartWithUntimedLast()
   {
      var later = CreateActivity(
         "Later",
         new DateTimeOffset(
            2026,
            7,
            25,
            12,
            0,
            0,
            TimeSpan.Zero
         )
      );
      var earlier = CreateActivity(
         "Earlier",
         new DateTimeOffset(
            2026,
            7,
            25,
            10,
            0,
            0,
            TimeSpan.Zero
         )
      );
      var untimed = CreateActivity("Untimed", null);
      var method = typeof(ActivityIndexPageService).GetMethod(
         "SortActivities",
         BindingFlags.NonPublic | BindingFlags.Static
      );

      var ascending = (IReadOnlyList<ActivityListItem>)method!.Invoke(
         null,
         [new[] { later, untimed, earlier }, "Time", true]
      )!;
      var descending = (IReadOnlyList<ActivityListItem>)method.Invoke(
         null,
         [new[] { later, untimed, earlier }, "Time", false]
      )!;

      Assert.Equal(
         ["Earlier", "Later", "Untimed"],
         ascending.Select(activity => activity.Title)
      );
      Assert.Equal(
         ["Later", "Earlier", "Untimed"],
         descending.Select(activity => activity.Title)
      );
   }

   private static ActivityListItem CreateActivity(
      string title,
      DateTimeOffset? startsAt
   )
   {
      return new ActivityListItem(
         Guid.NewGuid(),
         title,
         null,
         null,
         "Match",
         "football",
         "Football",
         null,
         startsAt?.ToString("yyyy-MM-dd HH:mm") ?? "2026-07-25",
         startsAt,
         null,
         ActivityPublicationStatusIds.Published,
         string.Empty,
         [],
         string.Empty
      );
   }
}
