namespace SESport.Core.Domain;

public sealed record EntityEvidence(
   Source Source,
   Uri? Uri,
   string? Title,
   DateTimeOffset ObservedAt,
   string Summary
);
