using SESport.Core.AI.Models;

namespace SESport.Core.AI.Abstractions;

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
