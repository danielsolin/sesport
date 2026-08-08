namespace SESport.Data.Models;

public sealed record AdminDashboardSnapshot(
   IReadOnlyList<DashboardDateSummary> Dates,
   IReadOnlyList<DashboardActivityIssue> ActivityIssues,
   DashboardAiHealth AiHealth,
   DashboardImportHealth? ImportHealth,
   IReadOnlyList<TodoItem> Todos
);

public sealed record DashboardDateSummary(
   DateOnly Date,
   int VisibleBroadcastCount,
   int UnreviewedBroadcastCount,
   int PublishedActivityCount,
   int DraftActivityCount
);

public sealed record DashboardActivityIssue(
   Guid Id,
   DateOnly Date,
   string Title,
   string PublicationStatus,
   bool IsDraft,
   bool IsMissingDescription,
   bool HasNoParticipants,
   bool HasNoGroup,
   bool HasNoRelatedSource,
   bool HasMissingParticipantStartTime,
   bool HasParticipantMissingPersonData,
   DateOnly ParticipantActivityDate
);

public sealed record DashboardAiHealth(
   int PendingCount,
   int RunningCount,
   int StaleRunningCount,
   int FailedLast25HoursCount
);

public sealed record DashboardImportHealth(
   string SourceKey,
   string Status,
   int BroadcastCount,
   DateTimeOffset StartedAt,
   DateTimeOffset? FinishedAt
);
