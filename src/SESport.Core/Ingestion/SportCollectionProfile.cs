namespace SESport.Core.Ingestion;

public sealed record SportCollectionProfile(
   ExternalEntityId SportExternalId,
   TimeSpan ExpectedActivityDuration,
   TimeSpan PublicationBuffer,
   int ExpectedEntityLinkCount
);
