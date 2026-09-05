using SESport.Core.Formatting;
using SESport.Data.Models;
using SESport.Web.Pages;

namespace SESport.Core.Tests.Pages;

public class PublicActivityTimelineBuilderTests
{
   [Fact]
   public void BuildFutureSeparatesActivitiesByDisplayDate()
   {
      var now = new DateTimeOffset(
         2026,
         8,
         26,
         12,
         0,
         0,
         TimeSpan.Zero
      );
      var firstDate = new DateOnly(2026, 8, 27);
      var secondDate = new DateOnly(2026, 8, 29);
      var builder = new PublicActivityTimelineBuilder();
      var activities = new[]
      {
         CreateActivity(
            "First",
            firstDate,
            TimeZoneHelper.ToUtc(
               firstDate,
               new TimeOnly(10, 0),
               SportDay.TimeZoneId
            )
         ),
         CreateActivity(
            "Second",
            firstDate,
            TimeZoneHelper.ToUtc(
               firstDate,
               new TimeOnly(12, 0),
               SportDay.TimeZoneId
            )
         ),
         CreateActivity(
            "Third",
            secondDate,
            TimeZoneHelper.ToUtc(
               secondDate,
               new TimeOnly(10, 0),
               SportDay.TimeZoneId
            )
         )
      };

      var timeline = builder.BuildFuture(activities, now);

      Assert.Equal(
         ["Imorgon 27 augusti", "Lördag 29 augusti"],
         timeline.TimelineEntries
            .Where(entry => entry.IsDateSeparator)
            .Select(entry => entry.DateSeparatorLabel)
      );
      Assert.Equal(
         ["First", "Second", "Third"],
         timeline.TimelineEntries
            .Where(entry => entry.Section is not null)
            .Select(entry => entry.Section!.Activities[0].Title)
      );
      Assert.DoesNotContain(
         timeline.TimelineEntries,
         entry => entry.IsCurrentMarker
      );
   }

   [Fact]
   public void BuildFutureLabelsTodayAsIdag()
   {
      var now = new DateTimeOffset(
         2026,
         8,
         26,
         12,
         0,
         0,
         TimeSpan.Zero
      );
      var today = SportDay.GetSportDate(now);
      var tomorrow = today.AddDays(1);
      var builder = new PublicActivityTimelineBuilder();
      var activities = new[]
      {
         CreateActivity(
            "Today",
            today,
            TimeZoneHelper.ToUtc(
               today,
               new TimeOnly(18, 0),
               SportDay.TimeZoneId
            )
         ),
         CreateActivity(
            "Tomorrow",
            tomorrow,
            TimeZoneHelper.ToUtc(
               tomorrow,
               new TimeOnly(10, 0),
               SportDay.TimeZoneId
            )
         )
      };

      var timeline = builder.BuildFuture(activities, now);
      var separators = timeline.TimelineEntries
         .Where(entry => entry.IsDateSeparator)
         .ToArray();

      Assert.Equal(
         ["Idag", "Imorgon 27 augusti"],
         separators.Select(entry => entry.DateSeparatorLabel)
      );
      Assert.True(separators[0].IsTodayDateSeparator);
      Assert.False(separators[1].IsTodayDateSeparator);
   }

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
         )
      };

      var timeline = builder.Build(activities, selectedDate, now);
      Assert.True(timeline.HasVisibleActivities);
      Assert.Equal(3, timeline.TimelineEntries.Count);
      Assert.Equal(
         "Past",
         timeline.TimelineEntries[0].Section!.Activities[0].Title
      );
      Assert.True(timeline.TimelineEntries[1].IsCurrentMarker);
      Assert.Equal(
         "Nu 14:00",
         timeline.TimelineEntries[1].CurrentMarkerLabel
      );
      Assert.Equal(
         "Future",
         timeline.TimelineEntries[2].Section!.Activities[0].Title
      );
   }

   [Fact]
   public void BuildExcludesActivitiesWithoutCompleteTimeRange()
   {
      var now = new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero);
      var selectedDate = SportDay.GetSportDate(now);
      var builder = new PublicActivityTimelineBuilder();
      var activities = new[]
      {
         CreateActivity(
            "Complete",
            selectedDate,
            now.AddHours(1)
         ),
         CreateActivity(
            "Without end",
            selectedDate,
            now.AddHours(2),
            omitEndTime: true
         ),
         CreateActivity(
            "Without start",
            selectedDate,
            null
         )
      };

      var timeline = builder.Build(activities, selectedDate, now);

      Assert.True(timeline.HasVisibleActivities);
      Assert.Equal(
         ["Complete"],
         timeline.TimelineEntries
            .Where(entry => entry.Section is not null)
            .Select(entry => entry.Section!.Activities[0].Title)
      );

      var invalidTimeline = builder.Build(
         activities.Skip(1),
         selectedDate,
         now
      );
      Assert.False(invalidTimeline.HasVisibleActivities);
      Assert.Empty(invalidTimeline.TimelineEntries);
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
         ["≈18:00", "≈18:00"],
         timeline.TimelineEntries.Select(entry => entry.Section!.TimeLabel)
      );
   }

   [Fact]
   public void BuildOrdersParticipantsByStartTime()
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
      var selectedDate = SportDay.GetSportDate(now).AddDays(1);
      var activity = CreateActivity(
         "Golf tournament",
         selectedDate,
         now.AddHours(1)
      ) with
      {
         Participants =
         [
            CreateParticipant("Late", "12:30"),
            CreateParticipant("Early", "08:15"),
            CreateParticipant("No start time", null)
         ]
      };

      var builder = new PublicActivityTimelineBuilder();
      var timeline = builder.Build([activity], selectedDate, now);
      var section = Assert.Single(timeline.TimelineEntries).Section!;

      Assert.Equal(
         ["Early", "Late", "No start time"],
         section.Activities[0].Participants.Select(
            participant => participant.Name
         )
      );
   }

   [Fact]
   public void BuildPutsTierZeroFirstInGroupedParticipantList()
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
      var regularParticipant = CreateParticipant(
         "Regular participant",
         null,
         hasDiscipline: true,
         disciplineAliasName: "100 m"
      ) with
      {
         WatchPriority = 30
      };
      var tierZeroParticipant = CreateParticipant(
         "Tier zero participant",
         null,
         hasDiscipline: true,
         disciplineAliasName: "200 m"
      ) with
      {
         WatchPriority = 0
      };
      var activities = new[]
      {
         CreateActivity(
            "First round",
            selectedDate,
            now.AddMinutes(15),
            activityGroupId: groupId,
            activityGroupTitle: "Tournament"
         ) with
         {
            Participants = [regularParticipant]
         },
         CreateActivity(
            "Second round",
            selectedDate,
            now.AddHours(1),
            activityGroupId: groupId,
            activityGroupTitle: "Tournament"
         ) with
         {
            Participants = [tierZeroParticipant]
         }
      };

      var builder = new PublicActivityTimelineBuilder();
      var timeline = builder.Build(activities, selectedDate, now);
      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      Assert.Equal(
         ["Tier zero participant", "Regular participant"],
         section.Participants.Select(participant => participant.Name)
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
            "Rally Polen",
            false,
            "SVT1, TV4"
         ),
         CreateActivity(
            "Rally Polen: Sträcka 10 - 11",
            selectedDate,
            now.AddHours(1).AddMinutes(45),
            now.AddHours(3).AddMinutes(25),
            groupId,
            "Rally Polen",
            false,
            "Eurosport"
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
      Assert.Equal("Rally Polen", section.DisplayTitle);
      Assert.Equal(3, section.Activities.Count);
      Assert.Equal(
         ["08:15", "09:45", "13:00"],
         section.Slots.Select(slot => slot.StartTimeLabel)
      );
      Assert.Equal(
         ["08:55", "11:25", "14:00"],
         section.Slots.Select(slot => slot.EndTimeLabel)
      );
      Assert.Equal(["SVT1", "TV4"], section.Slots[0].TvChannels);
      Assert.Equal(["Eurosport"], section.Slots[1].TvChannels);
      Assert.Empty(section.Slots[2].TvChannels);
      Assert.Equal("≈08:15", section.TimeLabel);

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
      Assert.Equal("≈08:15", activeSection.TimeLabel);

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

      Assert.Equal("≈08:15", middaySection.TimeLabel);
   }

   [Fact]
   public void Build_GroupsActivitiesAcrossStoredDatesInVisibleWindow()
   {
      var now = new DateTimeOffset(
         2026,
         7,
         26,
         22,
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
            "Race start",
            selectedDate,
            new DateTimeOffset(
               2026,
               7,
               26,
               23,
               45,
               0,
               TimeSpan.FromHours(2)
            ),
            activityGroupId: groupId,
            activityGroupTitle: "One Water Race"
         ),
         CreateActivity(
            "Day 2",
            selectedDate.AddDays(1),
            new DateTimeOffset(
               2026,
               7,
               27,
               0,
               0,
               0,
               TimeSpan.FromHours(2)
            ),
            activityGroupId: groupId,
            activityGroupTitle: "One Water Race"
         )
      };

      var timeline = builder.Build(activities, selectedDate, now);

      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;
      Assert.Equal("One Water Race", section.ActivityGroupTitle);
      Assert.Equal(
         ["Race start", "Day 2"],
         section.Activities.Select(activity => activity.Title)
      );
   }

   [Fact]
   public void BuildKeepsLocalCalendarActivitiesOnTheirStartDates()
   {
      var now = new DateTimeOffset(
         2026,
         7,
         26,
         22,
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
            "Race start",
            selectedDate,
            new DateTimeOffset(
               2026,
               7,
               26,
               23,
               45,
               0,
               TimeSpan.FromHours(2)
            ),
            activityGroupId: groupId,
            activityGroupTitle: "One Water Race",
            publicDateMode: ActivityGroupPublicDateModeIds.LocalCalendarDate
         ),
         CreateActivity(
            "Day 2",
            selectedDate.AddDays(1),
            new DateTimeOffset(
               2026,
               7,
               27,
               0,
               0,
               0,
               TimeSpan.FromHours(2)
            ),
            activityGroupId: groupId,
            activityGroupTitle: "One Water Race",
            publicDateMode: ActivityGroupPublicDateModeIds.LocalCalendarDate
         )
      };

      var timeline = builder.Build(activities, selectedDate, now);
      var sections = timeline.TimelineEntries
         .Where(entry => !entry.IsCurrentMarker)
         .Select(entry => entry.Section!)
         .ToList();

      Assert.Equal(2, sections.Count);
      Assert.Equal(
         ["Race start", "Day 2"],
         sections.Select(section =>
            Assert.Single(section.Activities).Title
         )
      );
      Assert.All(
         sections,
         section => Assert.Null(section.ActivityGroupTitle)
      );
   }

   [Fact]
   public void BuildKeepsParticipantsBoundToGroupedActivities()
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
      var first = CreateActivity(
         "Round 1",
         selectedDate,
         now.AddMinutes(15),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants = [CreateParticipant("First", "08:15")]
      };
      var second = CreateActivity(
         "Round 2",
         selectedDate,
         now.AddHours(1),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants = [CreateParticipant("Second", "09:00")]
      };

      var builder = new PublicActivityTimelineBuilder();
      var timeline = builder.Build(
         [first, second],
         selectedDate,
         now
      );
      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      Assert.Equal(
         ["First"],
         section.Activities[0].Participants.Select(
            participant => participant.Name
         )
      );
      Assert.Equal(
         ["Second"],
         section.Activities[1].Participants.Select(
            participant => participant.Name
         )
      );
      Assert.Equal("08:15", section.Activities[0].Participants[0].StartTime);
      Assert.Equal("09:00", section.Activities[1].Participants[0].StartTime);
      Assert.Equal(
         ["First", "Second"],
         section.Participants.Select(participant => participant.Name)
      );
      Assert.All(
         section.Slots,
         slot => Assert.True(slot.ShowParticipantNames)
      );
   }

   [Fact]
   public void BuildHidesGroupedParticipantNamesWhenSetsAreEqual()
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
      var participant = CreateParticipant("Participant", "08:15");
      var first = CreateActivity(
         "Round 1",
         selectedDate,
         now.AddMinutes(15),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants = [participant]
      };
      var second = CreateActivity(
         "Round 2",
         selectedDate,
         now.AddHours(1),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants = [participant with { StartTime = "09:00" }]
      };

      var builder = new PublicActivityTimelineBuilder();
      var timeline = builder.Build(
         [first, second],
         selectedDate,
         now
      );
      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      Assert.All(
         section.Slots,
         slot => Assert.False(slot.ShowParticipantNames)
      );
   }

   [Fact]
   public void BuildHidesNamesWhenAnotherGroupedActivityHasNoParticipants()
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
      var first = CreateActivity(
         "Round 1",
         selectedDate,
         now.AddMinutes(15),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants = [CreateParticipant("Participant", "08:15")]
      };
      var second = CreateActivity(
         "Round 2",
         selectedDate,
         now.AddHours(1),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants = []
      };

      var builder = new PublicActivityTimelineBuilder();
      var timeline = builder.Build(
         [first, second],
         selectedDate,
         now
      );
      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      Assert.False(section.Slots[0].ShowParticipantNames);
   }

   [Fact]
   public void BuildHidesParticipantNamesAlreadyPresentInActivityTitle()
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
      var first = CreateActivity(
         "Åhman/Hellvig – Hölting Nilsson/Andersson",
         selectedDate,
         now.AddMinutes(15),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants =
         [
            CreateParticipant("David Åhman", null),
            CreateParticipant("Elmer Andersson", null),
            CreateParticipant("Jacob Hölting Nilsson", null),
            CreateParticipant("Jonatan Hellvig", null)
         ]
      };
      var second = CreateActivity(
         "Another round",
         selectedDate,
         now.AddHours(1),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants = [CreateParticipant("Other Person", null)]
      };

      var builder = new PublicActivityTimelineBuilder();
      var timeline = builder.Build(
         [first, second],
         selectedDate,
         now
      );
      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      Assert.False(section.Slots[0].ShowParticipantNames);
      Assert.True(section.Slots[1].ShowParticipantNames);
   }

   [Fact]
   public void BuildUsesActivitySpecificStartTimeInMergedList()
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
      var alex = CreateParticipant("Alex", null);
      var first = CreateActivity(
         "Tournament day",
         selectedDate,
         now.AddMinutes(15),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants = [alex]
      };
      var second = CreateActivity(
         "Follow Alex",
         selectedDate,
         now.AddHours(1),
         activityGroupId: groupId,
         activityGroupTitle: "Tournament"
      ) with
      {
         Participants = [alex with { StartTime = "17:40" }]
      };

      var builder = new PublicActivityTimelineBuilder();
      var timeline = builder.Build(
         [first, second],
         selectedDate,
         now
      );
      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      var participant = Assert.Single(section.Participants);
      Assert.Equal("Alex", participant.Name);
      Assert.Equal("17:40", participant.StartTime);
   }

   [Fact]
   public void Build_DoesNotGroupActivitiesWhenGroupRequestsIt()
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
            "Activity One",
            selectedDate,
            now.AddMinutes(15),
            activityGroupId: groupId,
            activityGroupTitle: "Separate Group"
         ) with
         {
            NoGrouping = true
         },
         CreateActivity(
            "Activity Two",
            selectedDate,
            now.AddHours(1),
            activityGroupId: groupId,
            activityGroupTitle: "Separate Group"
         ) with
         {
            NoGrouping = true
         }
      };

      var timeline = builder.Build(activities, selectedDate, now);
      var sections = timeline.TimelineEntries
         .Where(entry => !entry.IsCurrentMarker)
         .Select(entry => entry.Section!)
         .ToList();

      Assert.Equal(2, sections.Count);
      Assert.All(sections, section =>
      {
         Assert.Null(section.ActivityGroupTitle);
         Assert.Single(section.Activities);
      });
   }

   [Theory]
   [InlineData(8, 14, "≈08:14")]
   [InlineData(8, 15, "≈08:15")]
   [InlineData(23, 50, "≈23:50")]
   public void Build_KeepsApproximateTimesWithoutRounding(
      int hour,
      int minute,
      string expectedLabel
   )
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
      var activityStart = new DateTimeOffset(
         2026,
         7,
         26,
         hour,
         minute,
         0,
         TimeSpan.FromHours(2)
      );
      var builder = new PublicActivityTimelineBuilder();
      var timeline = builder.Build(
         [CreateActivity("Rounded", selectedDate, activityStart)],
         selectedDate,
         now
      );

      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      Assert.Equal(expectedLabel, section.TimeLabel);
   }

   [Fact]
   public void Build_DoesNotGroupTeamSportActivities()
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
            "Football: Match 1",
            selectedDate,
            now.AddMinutes(15),
            now.AddHours(1),
            groupId,
            "Football League",
            true
         ),
         CreateActivity(
            "Football: Match 2",
            selectedDate,
            now.AddHours(2),
            now.AddHours(3),
            groupId,
            "Football League",
            true
         )
      };

      var timeline = builder.Build(activities, selectedDate, now);
      var sections = timeline.TimelineEntries
         .Where(entry => !entry.IsCurrentMarker)
         .Select(entry => entry.Section!)
         .ToList();

      Assert.Equal(2, sections.Count);
      Assert.All(sections, section =>
      {
         Assert.Null(section.ActivityGroupTitle);
         Assert.Single(section.Activities);
      });
   }

   [Fact]
   public void BuildConsolidatesSameTeamSportTitleIntoRegularActivityCard()
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
            "Football: Match",
            selectedDate,
            now.AddHours(2),
            now.AddHours(4),
            groupId,
            "Football League",
            true,
            "Viaplay"
         ),
         CreateActivity(
            "Football: Match",
            selectedDate,
            now.AddHours(1),
            now.AddHours(5),
            groupId,
            "Football League",
            true,
            "V Sport Football"
         )
      };

      var timeline = builder.Build(activities, selectedDate, now);
      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      Assert.Null(section.ActivityGroupTitle);
      Assert.Equal(2, section.Activities.Count);
      Assert.Equal("≈09:00", section.TimeLabel);
      Assert.Equal("≈13:00", section.EndTimeLabel);
      Assert.Equal(
         ["V Sport Football", "Viaplay"],
         section.Slots.SelectMany(slot => slot.TvChannels)
      );
   }

   [Fact]
   public void BuildConsolidatesNonTeamSportChannelVariants()
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
            "Göransson/Galloway - Krajicek/Mektić",
            selectedDate,
            now.AddHours(2),
            now.AddHours(4),
            groupId,
            "Winston-Salem Open",
            tvChannelName: "TV4 Tennis",
            sportId: "tennis",
            sportName: "Tennis"
         ),
         CreateActivity(
            "Göransson/Galloway - Krajicek/Mektić",
            selectedDate,
            now.AddHours(1),
            now.AddHours(5),
            groupId,
            "Winston-Salem Open",
            tvChannelName: "TV4 Play",
            sportId: "tennis",
            sportName: "Tennis"
         )
      };

      var timeline = builder.Build(activities, selectedDate, now);
      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      Assert.Null(section.ActivityGroupTitle);
      Assert.Equal(
         "Göransson/Galloway - Krajicek/Mektić",
         section.DisplayTitle
      );
      Assert.Equal(2, section.Activities.Count);
      Assert.Equal(
         ["TV4 Play", "TV4 Tennis"],
         section.Slots.SelectMany(slot => slot.TvChannels)
      );

      var sportCounts = IndexModel.CountActivityCardsBySport(timeline);

      var count = Assert.Single(sportCounts);
      Assert.Equal("tennis", count.SportId);
      Assert.Equal("Tennis", count.SportName);
      Assert.Equal(1, count.ParticipantCount);
      Assert.Empty(count.Countries);
   }

   [Fact]
   public void BuildKeepsDifferentParticipantVariantsGroupedNormally()
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
      var firstParticipantId = Guid.NewGuid();
      var secondParticipantId = Guid.NewGuid();
      var builder = new PublicActivityTimelineBuilder();
      var firstActivity = CreateActivity(
         "Göransson/Galloway - Krajicek/Mektić",
         selectedDate,
         now.AddHours(2),
         now.AddHours(4),
         groupId,
         "Winston-Salem Open",
         tvChannelName: "TV4 Tennis",
         sportId: "tennis",
         sportName: "Tennis"
      ) with
      {
         RelatedPersonEntities = "First participant",
         RelatedPersonEntityIds = [firstParticipantId],
         ActiveRelatedPersonEntityIds = [firstParticipantId]
      };
      var secondActivity = CreateActivity(
         "Göransson/Galloway - Krajicek/Mektić",
         selectedDate,
         now.AddHours(1),
         now.AddHours(5),
         groupId,
         "Winston-Salem Open",
         tvChannelName: "TV4 Play",
         sportId: "tennis",
         sportName: "Tennis"
      ) with
      {
         RelatedPersonEntities = "Second participant",
         RelatedPersonEntityIds = [secondParticipantId],
         ActiveRelatedPersonEntityIds = [secondParticipantId]
      };

      var timeline = builder.Build(
         [firstActivity, secondActivity],
         selectedDate,
         now
      );
      var section = Assert.Single(
         timeline.TimelineEntries,
         entry => !entry.IsCurrentMarker
      ).Section!;

      Assert.Equal("Winston-Salem Open", section.ActivityGroupTitle);
      Assert.Equal(2, section.Activities.Count);
   }

   private static ActivityListItem CreateActivity(
      string title,
      DateOnly activityDate,
      DateTimeOffset? startsAt,
      DateTimeOffset? endsAt = null,
      Guid? activityGroupId = null,
      string? activityGroupTitle = null,
      bool isTeamSport = false,
      string? tvChannelName = null,
      string publicDateMode = ActivityGroupPublicDateModeIds.SportDay,
      string sportId = "football",
      string sportName = "Football",
      bool omitEndTime = false
   )
   {
      var resolvedEndsAt = omitEndTime
         ? null
         : endsAt ?? startsAt?.AddHours(1);
      return new ActivityListItem(
         Guid.NewGuid(),
         title,
         null,
         null,
         "Match",
         sportId,
         sportName,
         null,
         startsAt is null
            ? activityDate.ToString(DateDisplay.DateOnlyFormat)
            : $"{activityDate:" + DateDisplay.DateOnlyFormat + $"}" +
              $"{startsAt.Value:" + DateDisplay.TimeOnlyMinutesFormat + $"}",
         startsAt,
         tvChannelName,
         "Published",
         string.Empty,
         [],
         string.Empty
      )
      {
         EndsAt = resolvedEndsAt,
         LocalEndTime = resolvedEndsAt is null
            ? null
            : TimeOnly.FromDateTime(
               TimeZoneHelper.ToLocal(
                  resolvedEndsAt.Value,
                  SportDay.TimeZoneId
               ).DateTime
            ),
         ActivityGroupId = activityGroupId,
         ActivityGroupTitle = activityGroupTitle,
         IsTeamSport = isTeamSport,
         ActivityDate = activityDate,
         PublicDateMode = publicDateMode
      };
   }

   private static PublicActivityParticipant CreateParticipant(
      string name,
      string? startTime,
      bool hasDiscipline = false,
      string? disciplineAliasName = null
   )
   {
      return new PublicActivityParticipant(
         Guid.NewGuid(),
         name,
         startTime,
         null,
         null,
         string.Empty,
         null,
         null,
         true,
         hasDiscipline,
         disciplineAliasName
      );
   }
}
