using SESport.Core.Sources;

namespace SESport.Data.Models;

public sealed record ActivityParticipantAiResultValueRecord(
   Guid Id,
   Guid EntityId,
   string EntityName,
   string FieldKey,
   string? ValueText,
   string ValueJson,
   IReadOnlyList<SourceEvidenceDraft> Sources
);

public sealed record ActivityParticipantAiResultSetRecord(
   string JobId,
   string JobLabel,
   Guid? RunId,
   string? RunStatusId,
   string? ResultSummary,
   DateTimeOffset? StartedAt,
   DateTimeOffset? CompletedAt,
   DateTimeOffset CreatedAt,
   DateTimeOffset UpdatedAt,
   IReadOnlyList<SourceEvidenceDraft> CheckedSources,
   IReadOnlyList<ActivityParticipantAiResultValueRecord> Values
);
