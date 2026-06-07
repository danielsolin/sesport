using SESport.AI.Models;

namespace SESport.AI.Abstractions;

public interface IAiJobRunRepository
{
   Task StoreAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   );

   Task UpdateAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   );
}
