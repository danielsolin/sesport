using SESport.Core.AI;

namespace SESport.AI.Interfaces;

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
