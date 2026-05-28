namespace SESport.Core.Tests;

public class EventIngestionServiceTests
{
   private static readonly Country Sweden =
      new(new CountryId("country:se"), "SE", "Sweden");

   private static readonly Country Switzerland =
      new(new CountryId("country:ch"), "CH", "Switzerland");

   [Fact]
   public void ImportedNationalTeamEventCreatesCountryConnection()
   {
      var importedEvent = CreateImportedSwedenVsSwitzerlandEvent();
      var ingestionService = new EventIngestionService();

      var sportEvent = ingestionService.ImportEvent(
         importedEvent,
         [Sweden, Switzerland]
      );

      var relevance = sportEvent.GetRelevanceFor(Sweden).Single();

      Assert.Equal("Sweden vs Switzerland", sportEvent.Name);
      Assert.Equal(
         "2026 IIHF Ice Hockey World Championship",
         sportEvent.Competition.Name
      );
      Assert.Equal(
         "Sweden men's national ice hockey team represents Sweden.",
         relevance.Reason
      );
   }

   private static ImportedEvent CreateImportedSwedenVsSwitzerlandEvent()
   {
      var source = new Source(
         new SourceId("source:test-iihf"),
         "Test IIHF source"
      );
      var iceHockey = new ImportedSport(
         new ExternalEntityId("ice-hockey"),
         "Ice hockey"
      );
      var competition = new ImportedCompetition(
         new ExternalEntityId("iihf-world-championship-2026"),
         "2026 IIHF Ice Hockey World Championship",
         iceHockey
      );

      return new ImportedEvent(
         source,
         new ExternalEntityId("iihf-2026-sweden-switzerland"),
         "Sweden vs Switzerland",
         competition,
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         "Quarter-final",
         [
            new ImportedParticipant(
               new ExternalEntityId("sweden-mens-ice-hockey"),
               "Sweden men's national ice hockey team",
               ParticipantKind.NationalTeam,
               new ImportedCountry(
                  new ExternalEntityId("se"),
                  "SE",
                  "Sweden"
               )
            ),
            new ImportedParticipant(
               new ExternalEntityId("switzerland-mens-ice-hockey"),
               "Switzerland men's national ice hockey team",
               ParticipantKind.NationalTeam,
               new ImportedCountry(
                  new ExternalEntityId("ch"),
                  "CH",
                  "Switzerland"
               )
            )
         ]
      );
   }
}
