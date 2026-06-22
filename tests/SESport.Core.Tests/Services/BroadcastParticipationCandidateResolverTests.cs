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
            10,
            PersonGenderIds.Female
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Other Person",
            TrackedEntityTypeIds.Person,
            "Cycling",
            "Some Other Tour",
            20,
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
            10,
            PersonGenderIds.Female
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Erik Karlsson",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "Some Tour",
            20,
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
            10,
            PersonGenderIds.Female
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Erik Karlsson",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "Some Tour",
            20,
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
            10,
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
            10,
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

   [Fact]
   public void CreateCandidatesTextSortsTierOneBeforeTierTwo()
   {
      var candidates = new[]
      {
         new EntityOption(
            Guid.NewGuid(),
            "Alfa",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "Tier 1 Tour",
            10,
            null
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Alfa 2024",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "Tier 2 Tour",
            20,
            null
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "Alfa 2024",
            null,
            ["Tennis"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal($"  - Alfa{Environment.NewLine}  - Alfa 2024", text);
   }

   [Fact]
   public void CreateCandidatesTextReturnsMoreThanFiveMatches()
   {
      var candidates = new[]
      {
         CreateCandidate("Candidate 1", 10, "Candidate"),
         CreateCandidate("Candidate 2", 20, "Candidate"),
         CreateCandidate("Candidate 3", 30, "Candidate"),
         CreateCandidate("Candidate 4", 40, "Candidate"),
         CreateCandidate("Candidate 5", 50, "Candidate"),
         CreateCandidate("Candidate 6", 60, "Candidate")
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "Candidate",
            null,
            ["Tennis"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      var lines = text.Split(
         Environment.NewLine,
         StringSplitOptions.RemoveEmptyEntries
      );

      Assert.Equal(6, lines.Length);
   }

   [Fact]
   public void CreateCandidatesTextMatchesAliasName()
   {
      var candidates = new[]
      {
         new EntityOption(
            Guid.NewGuid(),
            "Daniela Holmqvist",
            TrackedEntityTypeIds.Person,
            "Golf",
            "LPGA Tour",
            10,
            PersonGenderIds.Female,
            "Dani Holmqvist"
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "Dani Holmqvist at the LPGA",
            null,
            ["Golf"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal("  - Daniela Holmqvist", text);
   }

   private static EntityOption CreateCandidate(
      string name,
      int sortOrder,
      string organization
   )
   {
      return new EntityOption(
         Guid.NewGuid(),
         name,
         TrackedEntityTypeIds.Person,
         "Tennis",
         organization,
         sortOrder,
         null
      );
   }
}
