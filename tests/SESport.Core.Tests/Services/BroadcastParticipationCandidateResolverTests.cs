using SESport.Core.Domain;
using SESport.Core.Broadcast;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Core.Tests.Services;

public sealed class BroadcastParticipationCandidateResolverTests
{
   [Fact]
   public void CreateCandidatesTextMatchesTourLinkedPerson()
   {
      var candidates = new[]
      {
         new EntityOption(
            Guid.NewGuid(),
            "Jenny Rissveds",
            TrackedEntityTypeIds.Person,
            "Cycling",
            "UCI Mountain Bike World Series",
            PersonGenderIds.Female
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Other Person",
            TrackedEntityTypeIds.Person,
            "Cycling",
            "Some Other Tour",
            PersonGenderIds.Male
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "UCI Mountain Bike World Series, Mountainbike Damer Elit " +
            "Downhill Lenzerheide",
            null,
            ["Cycling"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal("- Jenny Rissveds", text);
      Assert.DoesNotContain("Other Person", text);
   }

   [Fact]
   public void CreateCandidatesTextReturnsEmptyWhenNoMatchExists()
   {
      var candidates = new[]
      {
         new EntityOption(
            Guid.NewGuid(),
            "Jenny Rissveds",
            TrackedEntityTypeIds.Person,
            "Cycling",
            "Some Other Tour",
            PersonGenderIds.Female
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "Unrelated broadcast title",
            null,
            ["Cycling"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal(string.Empty, text);
   }
}
