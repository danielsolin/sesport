namespace SESport.Core;

public sealed record ExternalMapping(
   Source Source,
   ExternalEntityId ExternalId,
   InternalEntityReference InternalEntity
);
