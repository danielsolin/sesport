using System.Reflection;

using SESport.Data;
using SESport.Web.Pages;

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
   public void FormatBirthday_OmitsYear()
   {
      var birthday = IndexModel.FormatBirthday(
         new DateOnly(1973, 8, 13)
      );

      Assert.Equal("13 augusti", birthday);
   }

   [Fact]
   public void BuildDateOptions_UsesThreeDayWindow()
   {
      var today = new DateOnly(2026, 7, 24);
      var selectedDate = today;

      var method = typeof(IndexModel).GetMethod(
         "BuildDateOptions",
         BindingFlags.NonPublic | BindingFlags.Static
      );

      var options = (IReadOnlyList<DateOption>)method!.Invoke(
         null,
         [today, selectedDate]
      )!;

      Assert.Equal(3, options.Count);
      Assert.Equal(
         [today, today.AddDays(1), today.AddDays(2)],
         options.Select(option => DateOnly.Parse(option.Value))
      );
      Assert.Equal(
         [
            "Idag (24 juli)",
            "Imorgon (25 juli)",
            "Söndag (26 juli)"
         ],
         options.Select(option => option.Label)
      );
   }

   [Fact]
   public void BuildDateOptions_IncludesSelectedDateOutsideWindow()
   {
      var today = new DateOnly(2026, 7, 24);
      var selectedDate = new DateOnly(2026, 7, 27);
      var method = typeof(IndexModel).GetMethod(
         "BuildDateOptions",
         BindingFlags.NonPublic | BindingFlags.Static
      );

      var options = (IReadOnlyList<DateOption>)method!.Invoke(
         null,
         [today, selectedDate]
      )!;
      var selectedOption = Assert.Single(
         options,
         option => option.IsSelected
      );

      Assert.Equal("2026-07-27", selectedOption.Value);
      Assert.Equal("Måndag (27 juli)", selectedOption.Label);
      Assert.Equal(4, options.Count);
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
      );
   }
}
