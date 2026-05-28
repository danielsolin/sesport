using SESport.Sources.Iihf;

namespace SESport.Core.Tests;

public class IihfEventSourceImporterTests
{
   private static readonly Country Sweden =
      new(new CountryId("country:se"), "SE", "Sweden");

   private static readonly Country Switzerland =
      new(new CountryId("country:ch"), "CH", "Switzerland");

   [Fact]
   public async Task IihfImporterCanImportSwedenVsSwitzerland()
   {
      var scheduleClient = new InMemoryIihfScheduleClient(
         [CreateSwedenVsSwitzerlandGame()]
      );
      var importer = new IihfEventSourceImporter(scheduleClient);
      var request = new ImportRequest(
         new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.FromHours(2)),
         new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.FromHours(2))
      );

      var importRun = await importer.ImportEventsAsync(
         request,
         CancellationToken.None
      );
      var sportEvent = new EventIngestionService().ImportEvent(
         importRun.Events.Single(),
         [Sweden, Switzerland]
      );
      var connection = sportEvent.GetCountryConnectionsFor(Sweden).Single();

      Assert.Equal(ImportRunStatus.Completed, importRun.Status);
      Assert.Equal("Sweden vs Switzerland", sportEvent.Name);
      Assert.Equal(
         "2026 IIHF Ice Hockey World Championship",
         sportEvent.Competition.Name
      );
      Assert.Equal(
         "Sweden men's national ice hockey team represents Sweden.",
         connection.Reason
      );
   }

   private static IihfGame CreateSwedenVsSwitzerlandGame()
   {
      return new IihfGame(
         "iihf-2026-sweden-switzerland",
         "iihf-world-championship-2026",
         "2026 IIHF Ice Hockey World Championship",
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         "Quarter-final",
         new IihfTeam(
            "sweden-mens-ice-hockey",
            "SE",
            "Sweden",
            "Sweden men's national ice hockey team"
         ),
         new IihfTeam(
            "switzerland-mens-ice-hockey",
            "CH",
            "Switzerland",
            "Switzerland men's national ice hockey team"
         )
      );
   }
}
