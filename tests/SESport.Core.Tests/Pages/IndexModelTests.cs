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
