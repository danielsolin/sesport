namespace SESport.Core.Ingestion;

public sealed class CollectionRefreshPlanner(
   SportCollectionProfile profile
)
{
   public DateTimeOffset? GetNextUsefulRefreshAt(ImportRun importRun)
   {
      var unresolvedProposals = importRun.Proposals
         .Where(IsForProfileSport)
         .Where(IsMissingExpectedEntityLinks)
         .ToList();

      if(unresolvedProposals.Count == 0)
      {
         return null;
      }

      var startsAt = unresolvedProposals
         .Select(GetStartsAt)
         .OfType<DateTimeOffset>()
         .ToList();

      if(startsAt.Count == 0)
      {
         return null;
      }

      return startsAt
         .Min()
         .Add(profile.ExpectedActivityDuration)
         .Add(profile.PublicationBuffer);
   }

   private bool IsForProfileSport(ActivityProposal proposal)
   {
      return proposal.Sport.ExternalId == profile.SportExternalId;
   }

   private bool IsMissingExpectedEntityLinks(ActivityProposal proposal)
   {
      return proposal.EntityLinks.Count < profile.ExpectedEntityLinkCount;
   }

   private static DateTimeOffset? GetStartsAt(ActivityProposal proposal)
   {
      return proposal.Time.StartsAt;
   }

}
