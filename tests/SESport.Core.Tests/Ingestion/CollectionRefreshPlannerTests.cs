namespace SESport.Core.Tests.Ingestion;

public class CollectionRefreshPlannerTests
{
   [Fact]
   public void PlannerReturnsNoRefreshWhenProposalsHaveExpectedEntityLinks()
   {
      var planner = CreateIceHockeyPlanner();
      var importRun = CreateImportRun(
         CreateProposal(
            new DateTimeOffset(2026, 5, 30, 15, 20, 0, TimeSpan.Zero),
            entityLinkCount: 2
         )
      );

      var nextRefreshAt = planner.GetNextUsefulRefreshAt(importRun);

      Assert.Null(nextRefreshAt);
   }

   [Fact]
   public void PlannerReturnsEarliestUsefulRefreshForUnresolvedProposals()
   {
      var planner = CreateIceHockeyPlanner();
      var startsAt = new DateTimeOffset(
         2026,
         5,
         30,
         15,
         20,
         0,
         TimeSpan.Zero
      );
      var importRun = CreateImportRun(
         CreateProposal(startsAt, entityLinkCount: 1)
      );

      var nextRefreshAt = planner.GetNextUsefulRefreshAt(importRun);

      Assert.Equal(
         startsAt.AddMinutes(165),
         nextRefreshAt
      );
   }

   private static CollectionRefreshPlanner CreateIceHockeyPlanner()
   {
      return new CollectionRefreshPlanner(
         new SportCollectionProfile(
            new ExternalEntityId("ice-hockey"),
            TimeSpan.FromMinutes(150),
            TimeSpan.FromMinutes(15),
            ExpectedEntityLinkCount: 2
         )
      );
   }

   private static ImportRun CreateImportRun(params ActivityProposal[] proposals)
   {
      return new ImportRun(
         new ImportRunId("import-run:test"),
         new Source(new SourceId("source:test"), "Test"),
         ImportRunStatus.Completed,
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow,
         proposals,
         []
      );
   }

   private static ActivityProposal CreateProposal(
      DateTimeOffset startsAt,
      int entityLinkCount
   )
   {
      var entityLinks = Enumerable
         .Range(1, entityLinkCount)
         .Select(index => new ActivityProposalEntityLink(
            EntityId.New(),
            ActivityEntityRole.CompetesIn,
            $"Entity {index} is connected to the activity.",
            $"Entity {index}",
            Confidence: 1.0m
         ))
         .ToList();

      return new ActivityProposal(
         new ActivityProposalId($"activity-proposal:{startsAt:yyyyMMddHHmmss}"),
         ActivityProposalProducerType.WebImport,
         new Source(new SourceId("source:test"), "Test"),
         new ExternalEntityId($"activity:{startsAt:yyyyMMddHHmmss}"),
         $"test:{startsAt:yyyyMMddHHmmss}",
         "Test activity",
         null,
         null,
         ActivityType.Match,
         new ImportedSport(
            new ExternalEntityId("ice-hockey"),
            "Ice hockey"
         ),
         "Test context",
         ActivityTime.Scheduled(startsAt),
         entityLinks,
         [],
         Confidence: 1.0m,
         ActivityProposalStatus.Pending,
         null,
         null
      );
   }
}
