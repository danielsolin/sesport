namespace SESport.Core.Ingestion;

public sealed record ImportedParticipant(
   ExternalEntityId ExternalId,
   string Name,
   ParticipantKind Kind,
   ImportedCountry? RepresentsCountry
);
