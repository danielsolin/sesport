namespace SESport.Core.Ingestion;

public sealed record ImportedCountry(
   ExternalEntityId ExternalId,
   string Code,
   string Name
);
