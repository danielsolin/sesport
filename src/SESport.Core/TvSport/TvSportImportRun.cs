namespace SESport.Core.TvSport;

public sealed record TvSportImportRun(
   Guid Id,
   string SourceKey,
   Uri? SourceUri,
   DateTimeOffset StartedAt,
   DateTimeOffset? FinishedAt,
   TvSportImportRunStatus Status,
   int BroadcastCount
);
