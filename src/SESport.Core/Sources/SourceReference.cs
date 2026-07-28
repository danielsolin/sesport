namespace SESport.Core.Sources;

public sealed record SourceReference(
   Guid Id,
   string CorrelationType,
   string CorrelationId,
   string Kind,
   string Url,
   string? Title,
   string? Excerpt,
   DateTimeOffset ObservedAt,
   DateTimeOffset CreatedAt
);
