namespace SESport.Core.Ingestion;

public sealed class CollectionRefreshPlanner(
   SportCollectionProfile profile
)
{
   public DateTimeOffset? GetNextUsefulRefreshAt(ImportRun importRun)
   {
      var unresolvedEvents = importRun.Events
         .Where(IsForProfileSport)
         .Where(IsMissingExpectedParticipants)
         .ToList();

      if(unresolvedEvents.Count == 0)
      {
         return null;
      }

      return unresolvedEvents
         .Min(e => e.StartsAt)
         .Add(profile.ExpectedEventDuration)
         .Add(profile.PublicationBuffer);
   }

   private bool IsForProfileSport(ImportedEvent importedEvent)
   {
      return importedEvent.Competition.Sport.ExternalId == profile.SportExternalId;
   }

   private bool IsMissingExpectedParticipants(ImportedEvent importedEvent)
   {
      return importedEvent.Participants.Count < profile.ExpectedParticipantCount;
   }
}
