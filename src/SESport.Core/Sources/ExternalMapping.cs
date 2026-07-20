namespace SESport.Core.Sources;

public sealed record ExternalMapping(
   IngestionSource Source,
   ExternalEntityId ExternalId,
   InternalEntityReference InternalEntity
);
