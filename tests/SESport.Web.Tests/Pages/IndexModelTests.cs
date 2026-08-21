using SESport.Data.Models;
using SESport.Web.Pages;
using System.Reflection;

namespace SESport.Core.Tests.Pages;

public sealed class IndexModelTests
{
   [Fact]
   public void CountParticipants_CountsUniqueEntityIds()
   {
      var activities = new[]
      {
         CreateActivity(
            "A",
            [Guid.Parse("11111111-1111-1111-1111-111111111111")]
         ),
         CreateActivity("B", []),
         CreateActivity(
            "C",
            [
               Guid.Parse("11111111-1111-1111-1111-111111111111"),
               Guid.Parse("22222222-2222-2222-2222-222222222222")
            ]
         )
      };

      var total = IndexModel.CountParticipants(activities);

      Assert.Equal(2, total);
   }

   [Fact]
   public void CountParticipants_UsesActiveEntityIds()
   {
      var inactiveId =
         Guid.Parse("11111111-1111-1111-1111-111111111111");
      var activeId = Guid.Parse(
         "22222222-2222-2222-2222-222222222222"
      );
      var activity = CreateActivity(
         "A",
         [inactiveId, activeId]
      ) with
      {
         ActiveRelatedPersonEntityIds = [activeId]
      };

      var total = IndexModel.CountParticipants([activity]);

      Assert.Equal(1, total);
   }

   [Fact]
   public void ShouldShowTomorrowLinkUsesTheSelectedDate()
   {
      var selectedDate = new DateOnly(2026, 8, 27);
      var publishedDateCounts = new[]
      {
         new PublishedDateParticipantCount(
            new DateOnly(2026, 8, 21),
            4
         )
      };

      Assert.False(
         IndexModel.ShouldShowTomorrowLink(
            selectedDate,
            publishedDateCounts
         )
      );
      Assert.True(
         IndexModel.ShouldShowTomorrowLink(
            selectedDate,
            [
               new PublishedDateParticipantCount(
                  selectedDate.AddDays(1),
                  4
               )
            ]
         )
      );
   }

   [Fact]
   public void ShouldShowDisciplineColumnHidesWhenValuesMatch()
   {
      var participants = new[]
      {
         CreateParticipant("Anna", "100 m"),
         CreateParticipant("Björn", "100 m")
      };

      var result = IndexModel.ShouldShowDisciplineColumn(participants);

      Assert.False(result);
   }

   [Fact]
   public void ShouldShowDisciplineColumnShowsWhenValuesDiffer()
   {
      var participants = new[]
      {
         CreateParticipant("Anna", "100 m"),
         CreateParticipant("Björn", "200 m")
      };

      var result = IndexModel.ShouldShowDisciplineColumn(participants);

      Assert.True(result);
   }

   [Fact]
   public void ShouldShowDisciplineColumnShowsWhenAValueIsMissing()
   {
      var participants = new[]
      {
         CreateParticipant("Anna", "100 m"),
         CreateParticipant("Björn", null)
      };

      var result = IndexModel.ShouldShowDisciplineColumn(participants);

      Assert.True(result);
   }

   [Theory]
   [InlineData(true, true, false, false, true)]
   [InlineData(true, true, true, false, false)]
   [InlineData(true, true, false, true, false)]
   [InlineData(false, true, false, false, false)]
   [InlineData(true, false, false, false, false)]
   public void ShouldAutoExpandPastActivitiesOnlyWhenTheDayIsFinished(
      bool isSportToday,
      bool hasPastActivities,
      bool hasActiveOrUpcomingActivities,
      bool hasUntimedActivities,
      bool expected
   )
   {
      var result = IndexModel.ShouldAutoExpandPastActivities(
         isSportToday,
         hasPastActivities,
         hasActiveOrUpcomingActivities,
         hasUntimedActivities
      );

      Assert.Equal(expected, result);
   }

   [Theory]
   [InlineData(0, true, false)]
   [InlineData(1, true, true)]
   [InlineData(0, false, false)]
   public void ShouldCollapseInactiveParticipantsRequiresActiveParticipants(
      int activeParticipantCount,
      bool hasInactiveParticipants,
      bool expected
   )
   {
      var result = IndexModel.ShouldCollapseInactiveParticipants(
         activeParticipantCount,
         hasInactiveParticipants
      );

      Assert.Equal(expected, result);
   }

   [Theory]
   [InlineData(true, true, true)]
   [InlineData(true, false, false)]
   [InlineData(false, true, false)]
   [InlineData(false, false, false)]
   public void ShouldCombineParticipantTogglesOnlyWhenBothListsCollapse(
      bool shouldCollapseParticipants,
      bool hasInactiveParticipants,
      bool expected
   )
   {
      var result = IndexModel.ShouldCombineParticipantToggles(
         shouldCollapseParticipants,
         hasInactiveParticipants
      );

      Assert.Equal(expected, result);
   }

   [Fact]
   public void CountParticipantsBySportCountsUniqueIdsWithinEachSport()
   {
      var firstPerson =
         Guid.Parse("11111111-1111-1111-1111-111111111111");
      var secondPerson =
         Guid.Parse("22222222-2222-2222-2222-222222222222");
      var activities = new[]
      {
         CreateActivity("Golf A", [firstPerson], "golf", "Golf"),
         CreateActivity(
            "Golf B",
            [firstPerson, secondPerson],
            "golf",
            "Golf"
         ),
         CreateActivity(
            "Tennis",
            [firstPerson],
            "tennis",
            "Tennis"
         )
      };

      var counts = IndexModel.CountParticipantsBySport(activities);

      Assert.Equal(2, counts.Count);
      Assert.Equal(
         new SportParticipantCount("golf", "Golf", 2),
         counts[0]
      );
      Assert.Equal(
         new SportParticipantCount("tennis", "Tennis", 1),
         counts[1]
      );
   }

   [Fact]
   public void CountParticipantsBySportSortsByParticipantCountDescending()
   {
      var firstPerson =
         Guid.Parse("11111111-1111-1111-1111-111111111111");
      var secondPerson =
         Guid.Parse("22222222-2222-2222-2222-222222222222");
      var activities = new[]
      {
         CreateActivity(
            "Golf",
            [firstPerson, secondPerson],
            "golf",
            "Golf"
         ),
         CreateActivity(
            "Athletics",
            [firstPerson],
            "athletics",
            "Athletics"
         )
      };

      var counts = IndexModel.CountParticipantsBySport(activities);

      Assert.Equal(
         ["golf", "athletics"],
         counts.Select(count => count.SportId)
      );
   }

   [Fact]
   public void FilterActivitiesBySportUsesSelectedSportOnly()
   {
      var activities = new[]
      {
         CreateActivity("Golf", [], "golf", "Golf"),
         CreateActivity("Tennis", [], "tennis", "Tennis")
      };

      var filtered = IndexModel.FilterActivitiesBySport(
         activities,
         "GOLF"
      );

      Assert.Single(filtered);
      Assert.Equal("Golf", filtered[0].Title);
      Assert.Equal(
         activities,
         IndexModel.FilterActivitiesBySport(activities, null)
      );
   }

   [Fact]
   public void SplitParticipantNames_TrimsAndSplitsNames()
   {
      var names = IndexModel.SplitParticipantNames(
         " Anna, Björn ,  Cecilia "
      );

      Assert.Equal(["Anna", "Björn", "Cecilia"], names);
   }

   [Fact]
   public void BuildDateOptions_UsesTodayAndPublishedFutureDates()
   {
      var today = new DateOnly(2026, 7, 24);
      var selectedDate = today;
      var publishedDateCounts = new[]
      {
         new PublishedDateParticipantCount(today.AddDays(-1), 4),
         new PublishedDateParticipantCount(today, 10),
         new PublishedDateParticipantCount(today.AddDays(2), 7),
         new PublishedDateParticipantCount(today.AddDays(5), 3)
      };

      var method = typeof(IndexModel).GetMethod(
         "BuildDateOptions",
         BindingFlags.NonPublic | BindingFlags.Static
      );

      var options = (IReadOnlyList<DateOption>)method!.Invoke(
         null,
         [today, selectedDate, publishedDateCounts]
      )!;

      Assert.Equal(3, options.Count);
      Assert.Equal(
         [today, today.AddDays(2), today.AddDays(5)],
         options.Select(option => DateOnly.Parse(option.Value))
      );
      Assert.Equal(
         [
            "Idag 24 juli",
            "Söndag 26 juli",
            "Onsdag 29 juli"
         ],
         options.Select(option => option.Label)
      );
      Assert.Equal(
         [10, 7, 3],
         options.Select(option => option.ParticipantCount)
      );
   }

   [Fact]
   public void BuildDateOptions_IncludesSelectedDateOutsideDefaultRange()
   {
      var today = new DateOnly(2026, 7, 24);
      var selectedDate = new DateOnly(2026, 7, 23);
      var method = typeof(IndexModel).GetMethod(
         "BuildDateOptions",
         BindingFlags.NonPublic | BindingFlags.Static
      );

      var options = (IReadOnlyList<DateOption>)method!.Invoke(
         null,
         [
            today,
            selectedDate,
            Array.Empty<PublishedDateParticipantCount>()
         ]
      )!;

      Assert.Equal(2, options.Count);
      var selectedOption = Assert.Single(
         options,
         option => option.IsSelected
      );
      Assert.Equal("2026-07-23", selectedOption.Value);
      Assert.Equal("Torsdag 23 juli", selectedOption.Label);
   }

   private static ActivityListItem CreateActivity(
      string title,
      Guid[] participantIds,
      string sportId = "football",
      string sportName = "Football"
   )
   {
      return new ActivityListItem(
         Guid.NewGuid(),
         title,
         null,
         null,
         "Match",
         sportId,
         sportName,
         null,
         "2026-06-26",
         null,
         null,
         "Published",
         string.Empty,
         participantIds,
         string.Empty
      )
      {
         ActiveRelatedPersonEntityIds = participantIds
      };
   }

   private static PublicActivityParticipant CreateParticipant(
      string name,
      string? discipline
   )
   {
      return new PublicActivityParticipant(
         Guid.NewGuid(),
         name,
         null,
         null,
         null,
         string.Empty,
         null,
         null,
         true,
         discipline is not null,
         discipline
      );
   }
}
