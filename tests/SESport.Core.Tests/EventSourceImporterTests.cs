namespace SESport.Core.Tests;

public class EventSourceImporterTests
{
   [Fact]
   public async Task EventSourceImporterCanProduceImportRun()
   {
      var source = new Source(
         new SourceId("source:test-iihf"),
         "Test IIHF source"
      );
      var importer = new FakeEventSourceImporter(source);
      var request = new ImportRequest(
         new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.Zero),
         new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.Zero)
      );

      var importRun = await importer.ImportEventsAsync(
         request,
         CancellationToken.None
      );

      Assert.Equal(source, importer.Source);
      Assert.Equal(source, importRun.Source);
      Assert.Equal(ImportRunStatus.Completed, importRun.Status);
      Assert.Single(importRun.Events);
   }

   private sealed class FakeEventSourceImporter(
      Source source
   ) : IEventSourceImporter
   {
      public Source Source { get; } = source;

      public Task<ImportRun> ImportEventsAsync(
         ImportRequest request,
         CancellationToken cancellationToken
      )
      {
         var importRun = new ImportRun(
            new ImportRunId("import-run:test-iihf-2026-05-28"),
            Source,
            ImportRunStatus.Completed,
            request.StartsAfter,
            request.StartsAfter.AddMinutes(1),
            [CreateImportedEvent(Source, request.StartsAfter.AddHours(20))],
            []
         );

         return Task.FromResult(importRun);
      }

      private static ImportedEvent CreateImportedEvent(
         Source source,
         DateTimeOffset startsAt
      )
      {
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
            startsAt,
            "Quarter-final",
            []
         );
      }
   }
}
