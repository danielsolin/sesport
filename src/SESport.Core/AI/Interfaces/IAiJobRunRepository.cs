namespace SESport.Core.AI;

public interface IAiJobRunRepository
{
   Task StoreAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   );

   Task<AiRunDetail?> GetRunAsync(
      Guid id,
      CancellationToken cancellationToken
   );

   Task<AiRunReference?> GetOriginatingActivityRunAsync(
      Guid activityId,
      CancellationToken cancellationToken
   );

   Task<Guid?> GetExistingRunIdAsync(
      string jobId,
      string correlationId,
      CancellationToken cancellationToken
   );

   Task<Guid?> GetActiveRunIdAsync(
      string jobId,
      string correlationId,
      CancellationToken cancellationToken
   );

   Task<bool> TryClaimRunAsync(
      Guid id,
      CancellationToken cancellationToken
   );

   Task<AiJobRunClaim?> ClaimNextRunAsync(
      IReadOnlyCollection<string> busyProviderIds,
      CancellationToken cancellationToken
   );

   Task DeleteRunAsync(
      Guid id,
      CancellationToken cancellationToken
   );

   Task<bool> ArchiveRunAsync(
      Guid id,
      CancellationToken cancellationToken
   );

   Task FailRunAsync(
      Guid id,
      string errorMessage,
      CancellationToken cancellationToken
   );

   Task UpdateToolTraceAsync(
      Guid runId,
      string? toolTraceJson,
      int toolRoundCount,
      CancellationToken cancellationToken
   );

   Task UpdateAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   );

   Task RecordApplicationAsync(
      Guid runId,
      string targetType,
      string targetId,
      CancellationToken cancellationToken
   );

   Task<int> FailStaleRunningRunsAsync(
      TimeSpan maxAge,
      CancellationToken cancellationToken
   );
}
