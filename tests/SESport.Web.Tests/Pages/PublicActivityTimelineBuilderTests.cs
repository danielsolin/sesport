using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Core.Tests.Pages;

public class PublicActivityTimelineBuilderTests
{
   [Fact]
   public void Build_ShowsEarlierAndFutureActivities()
   {
      var now = new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero);
      var selectedDate = SportDay.GetSportDate(now);
      var builder = new PublicActivityTimelineBuilder();
      var activities = new[]
      {
         CreateActivity(
            "Past",
            selectedDate,
            now.AddHours(-1)
         ),
         CreateActivity(
            "Future",
            selectedDate,
            now.AddHours(1)
         ),
         CreateActivity(
            "Untimed",
            selectedDate,
            null
         )
      };

      var timeline = builder.Build(activities, now);

      Assert.True(timeline.HasVisibleActivities);
      Assert.Equal(2, timeline.TimelineEntries.Count);
      Assert.Equal(
         "Past",
         timeline.TimelineEntries[0].Section.Activities[0].Title
      );
      Assert.Equal(
         "Future",
         timeline.TimelineEntries[1].Section.Activities[0].Title
      );
      Assert.Equal(
         ["Untimed"],
         timeline.UntimedActivities.Select(a => a.Title)
      );
   }

   [Fact]
   public void Build_ShowsAllActivitiesInTimeOrder()
   {
      var now = new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero);
      var selectedDate = SportDay.GetSportDate(now).AddDays(1);
      var builder = new PublicActivityTimelineBuilder();
      var activities = new[]
      {
         CreateActivity(
            "Earlier",
            selectedDate,
            now.AddHours(-1)
         ),
         CreateActivity(
            "Later",
            selectedDate,
            now.AddHours(1)
         )
      };

      var timeline = builder.Build(activities, now);

      Assert.True(timeline.HasVisibleActivities);
      Assert.Equal(
         ["Earlier", "Later"],
         timeline.TimelineEntries.Select(entry =>
            entry.Section.Activities[0].Title)
      );
   }

   [Fact]
   public void Build_MarksActivitiesThatAreOngoing()
   {
      var now = new DateTimeOffset(
         2026,
         6,
         12,
         12,
         0,
         0,
         TimeSpan.Zero
      );
      var selectedDate = SportDay.GetSportDate(now);
      var builder = new PublicActivityTimelineBuilder();
      var activities = new[]
      {
         CreateActivity(
            "Ended",
            selectedDate,
            now.AddHours(-2),
            now.AddHours(-1)
         ),
         CreateActivity(
            "Ongoing",
            selectedDate,
            now.AddMinutes(-30),
            now.AddMinutes(30)
         ),
         CreateActivity(
            "Upcoming",
            selectedDate,
            now.AddHours(1),
            now.AddHours(2)
         )
      };

      var timeline = builder.Build(activities, now);

      Assert.True(timeline.TimelineEntries[0].Section.HasEnded);
      Assert.True(timeline.TimelineEntries[1].Section.IsOngoing);
      Assert.False(timeline.TimelineEntries[2].Section.IsOngoing);
   }

   [Fact]
   public void Build_ShowsSeparateSectionsForSameStartTime()
   {
      var now = new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero);
      var selectedDate = SportDay.GetSportDate(now).AddDays(1);
      var builder = new PublicActivityTimelineBuilder();
      var sharedStart = now.AddHours(6);
      var activities = new[]
      {
         CreateActivity("First", selectedDate, sharedStart),
         CreateActivity("Second", selectedDate, sharedStart)
      };

      var timeline = builder.Build(activities, now);

      Assert.Equal(2, timeline.TimelineEntries.Count);
      Assert.Equal(
         ["First", "Second"],
         timeline.TimelineEntries.Select(entry =>
            entry.Section.Activities[0].Title)
      );
      Assert.All(
         timeline.TimelineEntries,
         entry => Assert.Single(entry.Section.Activities)
      );
      Assert.Equal(
         ["18:00", "18:00"],
         timeline.TimelineEntries.Select(entry => entry.Section.TimeLabel)
      );
   }

   private static ActivityListItem CreateActivity(
      string title,
      DateOnly activityDate,
      DateTimeOffset? startsAt,
      DateTimeOffset? endsAt = null
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
         startsAt is null
            ? activityDate.ToString("yyyy-MM-dd")
            : $"{activityDate:yyyy-MM-dd} {startsAt.Value:HH:mm}",
         startsAt,
         null,
         "Published",
         string.Empty,
         [],
         string.Empty
      )
      {
         EndsAt = endsAt,
         LocalEndTime = endsAt is null
            ? null
            : TimeOnly.FromDateTime(
               TimeZoneHelper.ToLocal(
                  endsAt.Value,
                  SportDay.TimeZoneId
               ).DateTime
            )
      };
   }
}
