namespace SESport.Core.Domain;

public sealed record TrackedEntity(
   EntityId Id,
   string CanonicalName,
   TrackedEntityType Type,
   Sport Sport,
   Country Country,
   CountryRelevanceKind CountryRelevanceKind,
   string CountryRelevanceReason
);
