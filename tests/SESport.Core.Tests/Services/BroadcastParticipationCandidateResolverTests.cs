using SESport.Core.Domain;
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
            "UCI Mountain Bike World Series"
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Other Person",
            TrackedEntityTypeIds.Person,
            "Cycling",
            "Some Other Tour"
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         "UCI Mountain Bike World Series, Mountainbike Damer Elit Downhill " +
         "Lenzerheide",
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
            "Some Other Tour"
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         "Unrelated broadcast title",
         candidates
      );

      Assert.Equal(string.Empty, text);
   }
}
