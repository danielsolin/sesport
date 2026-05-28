namespace SESport.Core.Ingestion;

public sealed record ImportedCompetition(
   ExternalEntityId ExternalId,
   string Name,
   ImportedSport Sport
);
