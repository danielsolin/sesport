namespace SESport.Core;

public sealed record ExternalMapping(
   Source Source,
   ExternalEntityId ExternalId,
   string InternalId
);
