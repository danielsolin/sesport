namespace SESport.Core.Tests.Ingestion;

public class ActivityProposalTests
{
   private static readonly Source Source =
      new(new SourceId("source:test"), "Test source");

   private static readonly ImportedSport IceHockey =
      new(new ExternalEntityId("ice-hockey"), "Ice hockey");

   [Theory]
   [InlineData(ActivityProposalProducerType.WebImport)]
   [InlineData(ActivityProposalProducerType.AiSearch)]
   [InlineData(ActivityProposalProducerType.Manual)]
   public void ProducersShareTheSameActivityProposalFormat(
      ActivityProposalProducerType producerType
   )
   {
      var proposal = CreateProposal(producerType);

      Assert.Equal(producerType, proposal.ProducerType);
      Assert.Equal(ActivityProposalStatus.Pending, proposal.Status);
      Assert.Equal(ActivityType.Match, proposal.Type);
      Assert.Single(proposal.EntityLinks);
      Assert.Single(proposal.Evidence);
   }

   [Fact]
   public void ActivityTimeSupportsExactStartDateRangeAndTbd()
   {
      var exact = ActivityTime.ExactStart(
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.Zero)
      );
      var range = ActivityTime.DateRange(
         new DateOnly(2026, 6, 1),
         new DateOnly(2026, 6, 7),
         "Tournament week"
      );
      var tbd = ActivityTime.ToBeDetermined("Draw not published.");

      Assert.Equal(ActivityTimeKind.ExactStart, exact.Kind);
      Assert.Equal(ActivityTimeKind.DateRange, range.Kind);
      Assert.Equal(ActivityTimeKind.ToBeDetermined, tbd.Kind);
      Assert.Equal("Draw not published.", tbd.Description);
   }

   [Fact]
   public void ApprovalCreatesCanonicalActivityWithLinksAndEvidence()
   {
      var proposal = CreateProposal(ActivityProposalProducerType.AiSearch);
      var activityId = ActivityId.New();
      var service = new ActivityProposalApprovalService();

      var activity = service.Approve(
         proposal,
         activityId,
         "This activity is relevant to Sweden because Tre Kronor participates."
      );
      var approvedProposal = service.MarkApproved(proposal, activityId);

      Assert.Equal(activityId, activity.Id);
      Assert.Equal(proposal.Title, activity.Title);
      Assert.Equal("Ice hockey", activity.Sport.Name);
      Assert.Single(activity.EntityLinks);
      Assert.Single(activity.Evidence);
      Assert.Equal(proposal.Id, activity.Evidence.Single().ProposalId);
      Assert.Equal(ActivityProposalStatus.Approved, approvedProposal.Status);
      Assert.Equal(activityId, approvedProposal.ActivityId);
   }

   [Fact]
   public void MultipleProposalsCanSupportTheSameCanonicalActivity()
   {
      var groupId = new ActivityProposalGroupId("activity-proposal-group:test");
      var firstProposal = CreateProposal(
         ActivityProposalProducerType.WebImport,
         new ActivityProposalId("activity-proposal:first"),
         groupId
      );
      var secondProposal = CreateProposal(
         ActivityProposalProducerType.Manual,
         new ActivityProposalId("activity-proposal:second"),
         groupId
      );
      var service = new ActivityProposalApprovalService();
      var activityId = ActivityId.New();

      var activity = service.Approve(
         firstProposal,
         activityId,
         "This activity is relevant to Sweden because Tre Kronor participates."
      );
      var mergedActivity = service.MergeEvidence(activity, secondProposal);

      Assert.Equal(2, mergedActivity.Evidence.Count);
      Assert.Contains(
         mergedActivity.Evidence,
         evidence => evidence.ProposalId == firstProposal.Id
      );
      Assert.Contains(
         mergedActivity.Evidence,
         evidence => evidence.ProposalId == secondProposal.Id
      );
   }

   private static ActivityProposal CreateProposal(
      ActivityProposalProducerType producerType,
      ActivityProposalId? id = null,
      ActivityProposalGroupId? groupId = null
   )
   {
      return new ActivityProposal(
         id ?? new ActivityProposalId("activity-proposal:test"),
         producerType,
         Source,
         new ExternalEntityId("external:test"),
         "test:fingerprint",
         "Sweden vs Switzerland",
         "IIHF quarter-final.",
         "Raw source payload",
         ActivityType.Match,
         IceHockey,
         "2026 IIHF Ice Hockey World Championship",
         ActivityTime.ExactStart(
            new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.Zero)
         ),
         [
            new ActivityProposalEntityLink(
               EntityId.New(),
               ActivityEntityRole.CompetesIn,
               "Tre Kronor participates in the match.",
               "Tre Kronor",
               Confidence: 0.95m
            )
         ],
         [
            new ActivityProposalEvidence(
               Source,
               new Uri("https://example.test/source"),
               "Schedule",
               new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero),
               "Schedule lists Sweden vs Switzerland.",
               "Sweden vs Switzerland"
            )
         ],
         Confidence: 0.95m,
         ActivityProposalStatus.Pending,
         groupId,
         null
      );
   }
}
