using SESport.Data;
using SESport.Web.Pages;

namespace SESport.Core.Tests.Pages;

public sealed class IndexModelTests
{
   [Fact]
   public void CountParticipants_SumsAllListedParticipants()
   {
      var activities = new[]
      {
         CreateActivity("A", "Anna, Björn"),
         CreateActivity("B", string.Empty),
         CreateActivity("C", "Cecilia, David, Erik")
      };

      var total = IndexModel.CountParticipants(activities);

      Assert.Equal(5, total);
   }

   private static ActivityListItem CreateActivity(
      string title,
      string participants
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
         participants,
         string.Empty
      );
   }
}
