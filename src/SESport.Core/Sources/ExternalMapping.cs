namespace SESport.Core.Sources;

public sealed record ExternalMapping(
   Source Source,
   ExternalEntityId ExternalId,
   InternalEntityReference InternalEntity
);
