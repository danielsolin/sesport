namespace SESport.Core.Ingestion;

public sealed record ImportedSport(
   ExternalEntityId ExternalId,
   string Name
);
