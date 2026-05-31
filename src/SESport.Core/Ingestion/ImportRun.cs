namespace SESport.Core.Ingestion;

public sealed record ImportRun(
   ImportRunId Id,
   Source Source,
   ImportRunStatus Status,
   DateTimeOffset StartedAt,
   DateTimeOffset? FinishedAt,
   IReadOnlyCollection<ActivityProposal> Proposals,
   IReadOnlyCollection<ImportIssue> Issues
);
