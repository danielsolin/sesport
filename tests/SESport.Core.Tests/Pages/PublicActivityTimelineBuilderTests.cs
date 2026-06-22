using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Core.Tests.Pages;

public class PublicActivityTimelineBuilderTests
{
   [Fact]
   public void Build_ShowsEarlierTodayAndPlacesCurrentMarker()
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

      var timeline = builder.Build(activities, selectedDate, now);
      var localNow = TimeZoneHelper.ToLocal(now, SportDay.TimeZoneId);

      Assert.True(timeline.HasVisibleActivities);
      Assert.Equal(3, timeline.TimelineEntries.Count);
      Assert.False(timeline.TimelineEntries[0].IsCurrentMarker);
      Assert.Equal(
         "Past",
         timeline.TimelineEntries[0].Section!.Activities[0].Title
      );
      Assert.True(timeline.TimelineEntries[1].IsCurrentMarker);
      Assert.Equal(
         $"Nu {localNow:HH:mm}",
         timeline.TimelineEntries[1].CurrentMarkerLabel
      );
      Assert.False(timeline.TimelineEntries[2].IsCurrentMarker);
      Assert.Equal(
         "Future",
         timeline.TimelineEntries[2].Section!.Activities[0].Title
      );
      Assert.Equal(
         ["Untimed"],
         timeline.UntimedActivities.Select(a => a.Title)
      );
   }

   [Fact]
   public void Build_ShowsAllActivitiesForOtherDatesWithoutMarker()
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

      var timeline = builder.Build(activities, selectedDate, now);

      Assert.True(timeline.HasVisibleActivities);
      Assert.All(
         timeline.TimelineEntries,
         entry => Assert.False(entry.IsCurrentMarker)
      );
      Assert.Equal(
         ["Earlier", "Later"],
         timeline.TimelineEntries.Select(entry =>
            entry.Section!.Activities[0].Title)
      );
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

      var timeline = builder.Build(activities, selectedDate, now);

      Assert.Equal(2, timeline.TimelineEntries.Count);
      Assert.Equal(
         ["First", "Second"],
         timeline.TimelineEntries.Select(entry =>
            entry.Section!.Activities[0].Title)
      );
      Assert.All(
         timeline.TimelineEntries,
         entry => Assert.Single(entry.Section!.Activities)
      );
      Assert.Equal(
         ["18:00", "18:00"],
         timeline.TimelineEntries.Select(entry => entry.Section!.TimeLabel)
      );
   }

   private static ActivityListItem CreateActivity(
      string title,
      DateOnly activityDate,
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
         startsAt is null
            ? activityDate.ToString("yyyy-MM-dd")
            : $"{activityDate:yyyy-MM-dd} {startsAt.Value:HH:mm}",
         startsAt,
         null,
         "Published",
         string.Empty,
         string.Empty
      );
   }
}
