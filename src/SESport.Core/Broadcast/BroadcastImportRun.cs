namespace SESport.Core.Broadcast;

public sealed record BroadcastImportRun(
   Guid Id,
   string SourceKey,
   Uri? SourceUri,
   DateTimeOffset StartedAt,
   DateTimeOffset? FinishedAt,
   BroadcastImportRunStatus Status,
   int BroadcastCount
);
