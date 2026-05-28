namespace SESport.Core.Ingestion;

public sealed record ImportRequest(
   DateTimeOffset StartsAfter,
   DateTimeOffset StartsBefore
);
