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
            ["Tennis"],
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

   [Fact]
   public void CreateCandidatesTextMatchesParenthesizedChampionshipName()
   {
      var candidates = new[]
      {
         CreateCandidate(
            "Oliver Solberg",
            10,
            "World Rally Championship (WRC)",
            "Motorsport"
         ),
         CreateCandidate(
            "Mille Johansson",
            20,
            "World Rally Championship (WRC)",
            "Motorsport"
         ),
         CreateCandidate(
            "Felix Rosenqvist",
            30,
            "Indycar Series",
            "Motorsport"
         )
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "World Rally Championship: Greece",
            null,
            ["Motorsport"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal(
         $"  - Oliver Solberg{Environment.NewLine}  - Mille Johansson",
         text
      );
      Assert.DoesNotContain("Felix Rosenqvist", text);
   }

   [Fact]
   public void CreateCandidatesTextFallsBackToSportMatches()
   {
      var candidates = new[]
      {
         CreateCandidate("Player 1", 30, "PDC / WDF Tournaments", "Darts"),
         CreateCandidate("Player 2", 10, "PDC / WDF Tournaments", "Darts"),
         CreateCandidate("Player 3", 20, "PDC / WDF Tournaments", "Darts")
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "U.S. Darts Masters",
            null,
            ["Darts"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal(
         $"  - Player 2{Environment.NewLine}" +
         "  - Player 3" +
         $"{Environment.NewLine}  - Player 1",
         text
      );
   }

   [Fact]
   public void CreateCandidatesTextPrefersAmateurOrganizationMatch()
   {
      var candidates = new[]
      {
         CreateCandidate(
            "Alice Player",
            10,
            "European Amateur Tour",
            "Golf"
         ),
         CreateCandidate("Bob Player", 20, "LPGA Tour", "Golf")
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "Amatör Open 2026",
            null,
            ["Golf"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      Assert.Equal("  - Alice Player", text);
      Assert.DoesNotContain("Bob Player", text);
   }

   [Fact]
   public void CreateCandidatesTextCapsFallbackToFiveNames()
   {
      var candidates = new[]
      {
         CreateCandidate("Player 1", 10, "PDC / WDF Tournaments", "Darts"),
         CreateCandidate("Player 2", 20, "PDC / WDF Tournaments", "Darts"),
         CreateCandidate("Player 3", 30, "PDC / WDF Tournaments", "Darts"),
         CreateCandidate("Player 4", 40, "PDC / WDF Tournaments", "Darts"),
         CreateCandidate("Player 5", 50, "PDC / WDF Tournaments", "Darts"),
         CreateCandidate("Player 6", 60, "PDC / WDF Tournaments", "Darts")
      };

      var text = BroadcastParticipationCandidateResolver.CreateCandidatesText(
         new BroadcastActivitySource(
            Guid.NewGuid(),
            "Channel",
            "U.S. Darts Masters",
            null,
            ["Darts"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
         ),
         candidates
      );

      var lines = text.Split(
         Environment.NewLine,
         StringSplitOptions.RemoveEmptyEntries
      );

      Assert.Equal(5, lines.Length);
      Assert.Equal("  - Player 1", lines[0]);
      Assert.Equal("  - Player 5", lines[^1]);
   }

   private static EntityOption CreateCandidate(
      string name,
      int sortOrder,
      string organization,
      string sport = "Tennis"
   )
   {
      return new EntityOption(
         Guid.NewGuid(),
         name,
         TrackedEntityTypeIds.Person,
         sport,
         organization,
         sortOrder,
         null
      );
   }
}
