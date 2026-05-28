namespace SESport.Core.Ingestion;

public sealed record ImportedEvent(
   Source Source,
   ExternalEntityId ExternalId,
   string Name,
   ImportedCompetition Competition,
   DateTimeOffset StartsAt,
   string Stage,
   IReadOnlyCollection<ImportedParticipant> Participants
);
