using Npgsql;

using SESport.Core.AI;
using SESport.Core.Broadcast;

namespace SESport.Data.AI;

public sealed class AiRepository(NpgsqlDataSource dataSource)
   : IAiJobDefinitionRepository, IAiJobRunRepository
{
   private readonly AiJobDefinitionRepository jobDefinitions = new(dataSource);
   private readonly AiJobRunRepository jobRuns = new(dataSource);
   private readonly AiRunApplicationRepository applications = new(dataSource);

   public Task<IReadOnlyList<AiRunListItem>> GetRunsAsync(
      DateOnly? date,
      string? jobId,
      IReadOnlyCollection<string>? statusIds,
      CancellationToken cancellationToken
   ) =>
      jobRuns.GetRunsAsync(date, jobId, statusIds, cancellationToken);

   public Task<IReadOnlyList<AiRunListItem>> GetRunsByIdsAsync(
      IReadOnlyCollection<Guid> ids,
      CancellationToken cancellationToken
   ) =>
      jobRuns.GetRunsByIdsAsync(ids, cancellationToken);

   public Task<AiRunDetail?> GetRunAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      jobRuns.GetRunAsync(id, cancellationToken);

   public Task<Guid?> GetExistingRunIdAsync(
      string jobId,
      string correlationId,
      CancellationToken cancellationToken
   ) =>
      jobRuns.GetExistingRunIdAsync(jobId, correlationId, cancellationToken);

   public Task<Guid?> GetActiveRunIdAsync(
      string jobId,
      string correlationId,
      CancellationToken cancellationToken
   ) =>
      jobRuns.GetActiveRunIdAsync(jobId, correlationId, cancellationToken);

   public Task<IReadOnlyList<CompletedActivityTeaserRun>>
      GetCompletedActivityTeaserRunsWithEmptyActivityTeasersAsync(
         int maxRuns,
         CancellationToken cancellationToken
      ) =>
      applications.GetCompletedActivityTeaserRunsWithEmptyActivityTeasersAsync(
         maxRuns,
         cancellationToken
      );

   public Task<IReadOnlyList<CompletedActivityGroupFactsRun>>
      GetUnappliedCompletedActivityGroupFactsRunsAsync(
         int maxRuns,
         CancellationToken cancellationToken
      ) =>
      applications.GetUnappliedCompletedActivityGroupFactsRunsAsync(
         maxRuns,
         cancellationToken
      );

   public Task<IReadOnlyList<Guid>>
      GetUnappliedCompletedActivityParticipantResultRunIdsAsync(
         int maxRuns,
         CancellationToken cancellationToken
      ) =>
      applications.GetUnappliedCompletedActivityParticipantResultRunIdsAsync(
         maxRuns,
         cancellationToken
      );

   public Task<IReadOnlyDictionary<Guid, BroadcastParticipationCheck>>
      GetParticipationChecksAsync(
         IReadOnlyCollection<Guid> broadcastIds,
         CancellationToken cancellationToken
      ) =>
      applications.GetParticipationChecksAsync(
         broadcastIds,
         cancellationToken
      );

   public Task<IReadOnlyDictionary<Guid, IReadOnlyList<
      BroadcastParticipationCheck>>>
      GetParticipationCheckHistoryAsync(
         IReadOnlyCollection<Guid> broadcastIds,
         CancellationToken cancellationToken
      ) =>
      applications.GetParticipationCheckHistoryAsync(
         broadcastIds,
         cancellationToken
      );

   public Task<AiJobDefinition?> GetJobAsync(
      string jobId,
      CancellationToken cancellationToken
   ) =>
      jobDefinitions.GetJobAsync(jobId, cancellationToken);

   public Task<AiPromptDefinition?> GetActivePromptAsync(
      string jobId,
      CancellationToken cancellationToken
   ) =>
      jobDefinitions.GetActivePromptAsync(jobId, cancellationToken);

   public Task<AiPromptDefinition?> GetPromptAsync(
      Guid promptId,
      CancellationToken cancellationToken
   ) =>
      jobDefinitions.GetPromptAsync(promptId, cancellationToken);

   public Task<AiProviderDefinition?> GetProviderAsync(
      string providerId,
      CancellationToken cancellationToken
   ) =>
      jobDefinitions.GetProviderAsync(providerId, cancellationToken);

   public Task StoreAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   ) =>
      jobRuns.StoreAsync(run, cancellationToken);

   public Task RecordApplicationAsync(
      Guid runId,
      string targetType,
      string targetId,
      CancellationToken cancellationToken
   ) =>
      jobRuns.RecordApplicationAsync(
         runId,
         targetType,
         targetId,
         cancellationToken
      );

   public Task<bool> TryClaimRunAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      jobRuns.TryClaimRunAsync(id, cancellationToken);

   public Task<AiJobRunClaim?> ClaimNextRunAsync(
      IReadOnlyCollection<string> busyProviderIds,
      CancellationToken cancellationToken
   ) =>
      jobRuns.ClaimNextRunAsync(busyProviderIds, cancellationToken);

   public Task DeleteRunAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      jobRuns.DeleteRunAsync(id, cancellationToken);

   public Task<bool> ArchiveRunAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      jobRuns.ArchiveRunAsync(id, cancellationToken);

   public Task FailRunAsync(
      Guid id,
      string errorMessage,
      CancellationToken cancellationToken
   ) =>
      jobRuns.FailRunAsync(id, errorMessage, cancellationToken);

   public Task UpdateAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   ) =>
      jobRuns.UpdateAsync(run, cancellationToken);

   public Task UpdateRunExecutionEnvironmentAsync(
      Guid runId,
      string? executionEnvironment,
      CancellationToken cancellationToken
   ) =>
      jobRuns.UpdateRunExecutionEnvironmentAsync(
         runId,
         executionEnvironment,
         cancellationToken
      );

   public Task<IReadOnlyList<string>> GetExecutionEnvironmentOptionsAsync(
      CancellationToken cancellationToken
   ) =>
      jobRuns.GetExecutionEnvironmentOptionsAsync(cancellationToken);

   public Task UpdateToolTraceAsync(
      Guid runId,
      string? toolTraceJson,
      int toolRoundCount,
      CancellationToken cancellationToken
   ) =>
      jobRuns.UpdateToolTraceAsync(
         runId,
         toolTraceJson,
         toolRoundCount,
         cancellationToken
      );

   public Task<int> FailStaleRunningRunsAsync(
      TimeSpan maxAge,
      CancellationToken cancellationToken
   ) =>
      jobRuns.FailStaleRunningRunsAsync(maxAge, cancellationToken);
}
