namespace SESport.Core.Ingestion;

public sealed class ActivityProposalApprovalService
{
   public Activity Approve(
      ActivityProposal proposal,
      ActivityId activityId
   )
   {
      return new Activity(
         activityId,
         proposal.Title,
         proposal.Description,
         proposal.Type,
         ToSport(proposal.Sport),
         proposal.Time,
         proposal.EntityLinks.Select(ToActivityLink).ToList(),
         proposal.Evidence.Select(evidence => ToActivityEvidence(
            proposal.Id,
            evidence
         )).ToList()
      );
   }

   public Activity MergeEvidence(
      Activity activity,
      ActivityProposal proposal
   )
   {
      var existingEvidence = activity.Evidence.ToList();
      var additionalEvidence = proposal.Evidence
         .Select(evidence => ToActivityEvidence(proposal.Id, evidence));

      return activity with
      {
         Evidence = existingEvidence.Concat(additionalEvidence).ToList()
      };
   }

   public ActivityProposal MarkApproved(
      ActivityProposal proposal,
      ActivityId activityId
   )
   {
      return proposal with
      {
         Status = ActivityProposalStatus.Approved,
         ActivityId = activityId,
         RejectReason = null,
         RejectComment = null
      };
   }

   public ActivityProposal Reject(
      ActivityProposal proposal,
      ActivityProposalRejectReason reason,
      string? comment = null
   )
   {
      return proposal with
      {
         Status = ActivityProposalStatus.Rejected,
         RejectReason = reason,
         RejectComment = string.IsNullOrWhiteSpace(comment)
            ? null
            : comment.Trim(),
         ActivityId = null
      };
   }

   private static ActivityEntityLink ToActivityLink(
      ActivityProposalEntityLink proposalLink
   )
   {
      return new ActivityEntityLink(
         proposalLink.EntityId
      );
   }

   private static ActivityEvidence ToActivityEvidence(
      ActivityProposalId proposalId,
      ActivityProposalEvidence proposalEvidence
   )
   {
      return new ActivityEvidence(
         proposalEvidence.Source,
         proposalEvidence.Uri,
         proposalEvidence.Title,
         proposalEvidence.ObservedAt,
         proposalEvidence.Summary,
         proposalId
      );
   }

   private static Sport ToSport(ImportedSport importedSport)
   {
      return new Sport(
         new SportId($"sport:{importedSport.ExternalId.Value}"),
         importedSport.Name
      );
   }
}
