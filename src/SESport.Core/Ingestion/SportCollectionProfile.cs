namespace SESport.Core.Ingestion;

public sealed record SportCollectionProfile(
   ExternalEntityId SportExternalId,
   TimeSpan ExpectedEventDuration,
   TimeSpan PublicationBuffer,
   int ExpectedParticipantCount
);
