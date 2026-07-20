namespace SESport.Core.Tests.Domain;

public class ImportRunTests
{
   [Fact]
   public void ImportRunCanKeepActivityProposalsAndIssuesTogether()
   {
      var source = new IngestionSource(
         new IngestionSourceId("source:test-iihf"),
         "Test IIHF source"
      );
      var proposal = CreateActivityProposal(source);
      var issue = new ImportIssue(
         ImportIssueKind.UnexpectedSourceShape,
         ImportIssueSeverity.Warning,
         proposal.ExternalId,
         "Venue was not available in the source payload."
      );

      var importRun = new ImportRun(
         new ImportRunId("import-run:test-iihf-2026-05-28"),
         source,
         ImportRunStatus.Completed,
         new DateTimeOffset(2026, 5, 28, 18, 0, 0, TimeSpan.Zero),
         new DateTimeOffset(2026, 5, 28, 18, 1, 0, TimeSpan.Zero),
         [proposal],
         [issue]
      );

      Assert.Equal(source, importRun.Source);
      Assert.Equal(ImportRunStatus.Completed, importRun.Status);
      Assert.Single(importRun.Proposals);
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

   private static ActivityProposal CreateActivityProposal(
      IngestionSource source
   )
   {
      var iceHockey = new ImportedSport(
         new ExternalEntityId("ice-hockey"),
         "Ice hockey"
      );
      return new ActivityProposal(
         new ActivityProposalId("activity-proposal:iihf-2026-sweden-switzerland"),
         ActivityProposalProducerType.WebImport,
         source,
         new ExternalEntityId("iihf-2026-sweden-switzerland"),
         "iihf:iihf-2026-sweden-switzerland",
         "Sweden vs Switzerland",
         "Quarter-final",
         null,
         ActivityType.Match,
         iceHockey,
         "2026 IIHF Ice Hockey World Championship",
         ActivityTime.Scheduled(
            new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2))
         ),
         [],
         [],
         Confidence: 1.0m,
         ActivityProposalStatus.Pending,
         null,
         null,
         null,
         null
      );
   }
}
