namespace SESport.Core.Ingestion;

public sealed record ImportRun(
   ImportRunId Id,
   Source Source,
   ImportRunStatus Status,
   DateTimeOffset StartedAt,
   DateTimeOffset? FinishedAt,
   IReadOnlyCollection<ImportedEvent> Events,
   IReadOnlyCollection<ImportIssue> Issues
);
