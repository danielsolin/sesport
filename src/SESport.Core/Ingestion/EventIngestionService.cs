namespace SESport.Core.Ingestion;

public sealed class EventIngestionService
{
   public SportEvent ImportEvent(
      ImportedEvent importedEvent,
      IReadOnlyCollection<Country> knownCountries
   )
   {
      return new SportEvent(
         ToEventId(importedEvent.ExternalId),
         importedEvent.Name,
         ToCompetition(importedEvent.Competition),
         importedEvent.StartsAt,
         importedEvent.Stage,
         importedEvent.Participants
            .Select(participant => ToParticipant(participant, knownCountries))
            .ToList()
      );
   }

   private static Competition ToCompetition(
      ImportedCompetition importedCompetition
   )
   {
      return new Competition(
         ToCompetitionId(importedCompetition.ExternalId),
         importedCompetition.Name,
         ToSport(importedCompetition.Sport)
      );
   }

   private static Sport ToSport(ImportedSport importedSport)
   {
      return new Sport(
         ToSportId(importedSport.ExternalId),
         importedSport.Name
      );
   }

   private static Participant ToParticipant(
      ImportedParticipant importedParticipant,
      IReadOnlyCollection<Country> knownCountries
   )
   {
      return new Participant(
         ToParticipantId(importedParticipant.ExternalId),
         importedParticipant.Name,
         importedParticipant.Kind,
         ResolveCountry(importedParticipant.RepresentsCountry, knownCountries)
      );
   }

   private static Country? ResolveCountry(
      ImportedCountry? importedCountry,
      IReadOnlyCollection<Country> knownCountries
   )
   {
      if (importedCountry is null)
      {
         return null;
      }

      return knownCountries.Single(
         country => country.Code == importedCountry.Code
      );
   }

   private static EventId ToEventId(ExternalEntityId externalId)
   {
      return new EventId($"event:{externalId.Value}");
   }

   private static CompetitionId ToCompetitionId(ExternalEntityId externalId)
   {
      return new CompetitionId($"competition:{externalId.Value}");
   }

   private static SportId ToSportId(ExternalEntityId externalId)
   {
      return new SportId($"sport:{externalId.Value}");
   }

   private static ParticipantId ToParticipantId(ExternalEntityId externalId)
   {
      return new ParticipantId($"participant:{externalId.Value}");
   }
}
