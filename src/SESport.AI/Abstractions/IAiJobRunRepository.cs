using SESport.AI.Models;

namespace SESport.AI.Abstractions;

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

   Task<bool> TryClaimRunAsync(
      Guid id,
      CancellationToken cancellationToken
   );

   Task<Guid?> ClaimNextRunAsync(
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
      CancellationToken cancellationToken
   );

   Task UpdateAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   );

   Task<int> FailStaleRunningRunsAsync(
      TimeSpan maxAge,
      CancellationToken cancellationToken
   );
}
