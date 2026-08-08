namespace SESport.Data.Models;

public sealed record TodoItem(
   Guid Id,
   string TargetTypeId,
   string Text,
   string? CorrelationId,
   DateTimeOffset CreatedAt
);
