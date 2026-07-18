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
         CreateActivity("A", [Guid.Parse("11111111-1111-1111-1111-111111111111")]),
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
   public void SplitParticipantNames_TrimsAndSplitsNames()
   {
      var names = IndexModel.SplitParticipantNames(
         " Anna, Björn ,  Cecilia "
      );

      Assert.Equal(["Anna", "Björn", "Cecilia"], names);
   }

   [Fact]
   public void BuildDateOptions_UsesThreeDayWindow()
   {
      var today = new DateOnly(2026, 7, 3);
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
         [today.AddDays(-1), today, today.AddDays(1)],
         options.Select(option => DateOnly.Parse(option.Value))
      );
   }

   private static ActivityListItem CreateActivity(
      string title,
      Guid[] participantIds
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
