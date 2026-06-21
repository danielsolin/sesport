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

      Assert.Equal("  - Jenny Rissveds", text);
      Assert.DoesNotContain("Other Person", text);
   }

   [Fact]
   public void CreateCandidatesTextKeepsFemaleEventsToFemaleCandidates()
   {
      var candidates = new[]
      {
         new EntityOption(
            Guid.NewGuid(),
            "Anna Svensson",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "Some Tour",
            PersonGenderIds.Female
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Erik Karlsson",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "Some Tour",
            PersonGenderIds.Male
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "Anna Svensson vs Erik Karlsson - Damallsvenskan",
            null,
            ["Tennis"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal("  - Anna Svensson", text);
      Assert.DoesNotContain("Erik Karlsson", text);
   }

   [Fact]
   public void CreateCandidatesTextKeepsMaleEventsToMaleCandidates()
   {
      var candidates = new[]
      {
         new EntityOption(
            Guid.NewGuid(),
            "Anna Svensson",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "Some Tour",
            PersonGenderIds.Female
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Erik Karlsson",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "Some Tour",
            PersonGenderIds.Male
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "Anna Svensson vs Erik Karlsson - Herrarnas SM",
            null,
            ["Tennis"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal("  - Erik Karlsson", text);
      Assert.DoesNotContain("Anna Svensson", text);
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

   [Fact]
   public void CreateCandidatesTextMatchesATPWorldTourVariant()
   {
      var candidates = new[]
      {
         new EntityOption(
            Guid.NewGuid(),
            "Novak Djokovic",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "ATP Tour",
            PersonGenderIds.Male
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "Tennis: ATP World Tour 250-turnering i Eastbourne",
            null,
            ["Tennis"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal("  - Novak Djokovic", text);
   }
}
