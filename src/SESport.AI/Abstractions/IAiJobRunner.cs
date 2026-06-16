using SESport.AI.Models;

namespace SESport.AI.Abstractions;

public interface IAiJobRunner
{
   Task<Guid> QueueAsync(
      AiJobRequest request,
      CancellationToken cancellationToken
   );

   Task<AiJobResult> RunAsync(
      AiJobRequest request,
      CancellationToken cancellationToken
   );
}
