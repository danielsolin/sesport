namespace SESport.Core.Tests;

public class ImportRunTests
{
   [Fact]
   public void ImportRunCanKeepImportedEventsAndIssuesTogether()
   {
      var source = new Source(
         new SourceId("source:test-iihf"),
         "Test IIHF source"
      );
      var importedEvent = CreateImportedEvent(source);
      var issue = new ImportIssue(
         ImportIssueKind.UnexpectedSourceShape,
         ImportIssueSeverity.Warning,
         importedEvent.ExternalId,
         "Venue was not available in the source payload."
      );

      var importRun = new ImportRun(
         new ImportRunId("import-run:test-iihf-2026-05-28"),
         source,
         ImportRunStatus.Completed,
         new DateTimeOffset(2026, 5, 28, 18, 0, 0, TimeSpan.Zero),
         new DateTimeOffset(2026, 5, 28, 18, 1, 0, TimeSpan.Zero),
         [importedEvent],
         [issue]
      );

      Assert.Equal(source, importRun.Source);
      Assert.Equal(ImportRunStatus.Completed, importRun.Status);
      Assert.Single(importRun.Events);
      Assert.Single(importRun.Issues);
      Assert.Equal(
         ImportIssueKind.UnexpectedSourceShape,
         importRun.Issues.Single().Kind
      );
      Assert.Equal(
         ImportIssueSeverity.Warning,
         importRun.Issues.Single().Severity
      );
   }

   private static ImportedEvent CreateImportedEvent(Source source)
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
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         "Quarter-final",
         []
      );
   }
}
