using SESport.Core.Formatting;
using SESport.Data.Models;
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

      var timeline = builder.Build(activities, selectedDate, now);
      var localNow = TimeZoneHelper.ToLocal(now, SportDay.TimeZoneId);

      Assert.True(timeline.HasVisibleActivities);
      Assert.Equal(3, timeline.TimelineEntries.Count);
      Assert.Equal(
         "Past",
         timeline.TimelineEntries[0].Section!.Activities[0].Title
      );
      Assert.True(timeline.TimelineEntries[1].IsCurrentMarker);
      Assert.Equal(
         $"Nu {localNow:HH:mm}",
         timeline.TimelineEntries[1].CurrentMarkerLabel
      );
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

      var timeline = builder.Build(activities, selectedDate, now);

      Assert.True(timeline.HasVisibleActivities);
      Assert.Equal(
         ["Earlier", "Later"],
         timeline.TimelineEntries.Select(entry =>
            entry.Section!.Activities[0].Title)
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

      var timeline = builder.Build(activities, selectedDate, now);

      Assert.True(timeline.TimelineEntries[0].Section!.HasEnded);
      Assert.True(timeline.TimelineEntries[1].IsCurrentMarker);
      Assert.True(timeline.TimelineEntries[2].Section!.IsOngoing);
      Assert.False(timeline.TimelineEntries[3].Section!.IsOngoing);
   }

   [Fact]
   public void Build_ShowsRecentlyEndedBeforeEarlierOngoingActivity()
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
            "Ongoing",
            selectedDate,
            now.AddHours(-2),
            now.AddHours(1)
         ),
         CreateActivity(
            "Recently ended",
            selectedDate,
            now.AddHours(-1),
            now.AddMinutes(-5)
         )
      };

      var timeline = builder.Build(activities, selectedDate, now);

      Assert.Equal(
         "Recently ended",
         timeline.TimelineEntries[0].Section!.Activities[0].Title
      );
      Assert.True(timeline.TimelineEntries[1].IsCurrentMarker);
      Assert.Equal(
         "Ongoing",
         timeline.TimelineEntries[2].Section!.Activities[0].Title
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

   [Fact]
   public void Build_GroupsActivitiesFromSameGroupAndDate()
   {
      var now = new DateTimeOffset(
         2026,
         7,
         26,
         8,
         0,
         0,
         TimeSpan.FromHours(2)
      );
      var selectedDate = new DateOnly(2026, 7, 26);
      var groupId = Guid.NewGuid();
      var builder = new PublicActivityTimelineBuilder();
      var activities = new[]
      {
         CreateActivity(
            "Rally Polen: Sträcka 9",
            selectedDate,
            now.AddMinutes(15),
            now.AddMinutes(55),
            groupId,
            "Rally Polen"
         ),
         CreateActivity(
            "Rally Polen: Sträcka 10 - 11",
            selectedDate,
            now.AddHours(1).AddMinutes(45),
            now.AddHours(3).AddMinutes(25),
            groupId,
            "Rally Polen"
         ),
         CreateActivity(
            "Rally Polen: Live Stage 1",
            selectedDate,
            now.AddHours(5),
            now.AddHours(6),
            groupId,
            "Rally Polen"
         )
      };

      var timeline = builder.Build(activities, selectedDate, now);

      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;
      Assert.Equal("Rally Polen", section.ActivityGroupTitle);
      Assert.Equal(3, section.Activities.Count);
      Assert.Equal(
         ["08:15", "09:45", "13:00"],
         section.Slots.Select(slot => slot.TimeLabel)
      );
      Assert.Equal(
         ["08:55", "11:25", "14:00"],
         section.Slots.Select(slot => slot.EndTimeLabel)
      );
      Assert.Equal("08:15", section.TimelineSlot.TimeLabel);

      var duringFirstActivity = new DateTimeOffset(
         2026,
         7,
         26,
         8,
         30,
         0,
         TimeSpan.FromHours(2)
      );
      var activeTimeline = builder.Build(
         activities,
         selectedDate,
         duringFirstActivity
      );
      var activeSection =
         Assert.Single(
            activeTimeline.TimelineEntries,
            entry => !entry.IsCurrentMarker
         ).Section!;

      Assert.True(activeSection.TimelineSlot.IsOngoing);
      Assert.Equal("08:15", activeSection.TimelineSlot.TimeLabel);

      var midday = new DateTimeOffset(
         2026,
         7,
         26,
         12,
         45,
         0,
         TimeSpan.FromHours(2)
      );
      var middayTimeline = builder.Build(
         activities,
         selectedDate,
         midday
      );
      var middaySection =
         Assert.Single(
            middayTimeline.TimelineEntries,
            entry => !entry.IsCurrentMarker
         ).Section!;

      Assert.Equal("13:00", middaySection.TimelineSlot.TimeLabel);
   }

   private static ActivityListItem CreateActivity(
      string title,
      DateOnly activityDate,
      DateTimeOffset? startsAt,
      DateTimeOffset? endsAt = null,
      Guid? activityGroupId = null,
      string? activityGroupTitle = null
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
            ),
         ActivityGroupId = activityGroupId,
         ActivityGroupTitle = activityGroupTitle
      };
   }
}
