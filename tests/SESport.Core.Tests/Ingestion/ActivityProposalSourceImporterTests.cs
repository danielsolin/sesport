namespace SESport.Core.Tests.Ingestion;

public class ActivityProposalSourceImporterTests
{
   [Fact]
   public async Task ActivityProposalSourceImporterCanProduceProposalImportRun()
   {
      var source = new Source(
         new SourceId("source:test-iihf"),
         "Test IIHF source"
      );
      var importer = new FakeActivityProposalSourceImporter(source);
      var request = new ImportRequest(
         new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.Zero),
         new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.Zero)
      );

      var importRun = await importer.ImportActivityProposalsAsync(
         request,
         CancellationToken.None
      );

      Assert.Equal(source, importer.Source);
      Assert.Equal(source, importRun.Source);
      Assert.Equal(ImportRunStatus.Completed, importRun.Status);
      Assert.Single(importRun.Proposals);
   }

   private sealed class FakeActivityProposalSourceImporter(
      Source source
   ) : IActivityProposalSourceImporter
   {
      public Source Source { get; } = source;

      public Task<ImportRun> ImportActivityProposalsAsync(
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
            [CreateActivityProposal(Source, request.StartsAfter.AddHours(20))],
            []
         );

         return Task.FromResult(importRun);
      }

      private static ActivityProposal CreateActivityProposal(
         Source source,
         DateTimeOffset startsAt
      )
      {
         var iceHockey = new ImportedSport(
            new ExternalEntityId("ice-hockey"),
            "Ice hockey"
         );
         return new ActivityProposal(
            new ActivityProposalId("activity-proposal:test"),
            ActivityProposalProducerType.WebImport,
            source,
            new ExternalEntityId("iihf-2026-sweden-switzerland"),
            "test:fingerprint",
            "Sweden vs Switzerland",
            "Quarter-final",
            null,
            ActivityType.Match,
            iceHockey,
            "2026 IIHF Ice Hockey World Championship",
            ActivityTime.Scheduled(startsAt),
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
}
