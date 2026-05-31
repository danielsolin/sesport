using SESport.Sources.Iihf;

namespace SESport.Core.Tests.Sources.Iihf;

public class IihfActivityProposalSourceImporterTests
{
   private static readonly Uri UnusedStatsUri =
      new("https://example.test/iihf/stats");

   [Fact]
   public async Task IihfImporterProducesActivityProposal()
   {
      var scheduleClient = new InMemoryIihfScheduleClient(
         [CreateSwedenVsSwitzerlandGame()]
      );
      var importer = new IihfActivityProposalSourceImporter(
         scheduleClient,
         CreateActivityContextSource()
      );
      var request = new ImportRequest(
         new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.FromHours(2)),
         new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.FromHours(2))
      );

      var importRun = await importer.ImportActivityProposalsAsync(
         request,
         CancellationToken.None
      );
      var proposal = importRun.Proposals.Single();

      Assert.Equal(ImportRunStatus.Completed, importRun.Status);
      Assert.Equal(ActivityProposalProducerType.WebImport, proposal.ProducerType);
      Assert.Equal(ActivityProposalStatus.Pending, proposal.Status);
      Assert.Equal(ActivityType.Match, proposal.Type);
      Assert.Equal("Sweden vs Switzerland", proposal.Title);
      Assert.Equal(ActivityTimeKind.ExactStart, proposal.Time.Kind);
      Assert.Equal(
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         proposal.Time.StartsAt
      );
      Assert.Equal("Ice hockey", proposal.Sport.Name);
      Assert.Equal("2026 IIHF Ice Hockey World Championship", proposal.Context);
      Assert.Equal(2, proposal.EntityLinks.Count);
      Assert.Contains(
         proposal.EntityLinks,
         link => link.ContextName == "Sweden men's national ice hockey team"
      );
      Assert.Single(proposal.Evidence);
      Assert.Equal(UnusedStatsUri, proposal.Evidence.Single().Uri);
   }

   [Fact]
   public async Task IihfImporterReportsIssueWhenNoEventsAreFound()
   {
      var scheduleClient = new InMemoryIihfScheduleClient([]);
      var importer = new IihfActivityProposalSourceImporter(
         scheduleClient,
         CreateActivityContextSource()
      );
      var request = new ImportRequest(
         new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.FromHours(2)),
         new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.FromHours(2))
      );

      var importRun = await importer.ImportActivityProposalsAsync(
         request,
         CancellationToken.None
      );

      var issue = importRun.Issues.Single();

      Assert.Empty(importRun.Proposals);
      Assert.Equal(ImportIssueKind.NoEventsFound, issue.Kind);
      Assert.Equal(ImportIssueSeverity.Warning, issue.Severity);
      Assert.Equal("No IIHF events were found for 2026/wm.", issue.Message);
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

   private static IihfActivityContextSource CreateActivityContextSource()
   {
      return new IihfActivityContextSource(
         "2026 IIHF Ice Hockey World Championship",
         "2026/wm",
         UnusedStatsUri
      );
   }
}
