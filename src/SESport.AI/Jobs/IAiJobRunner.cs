using SESport.Core.AI;

namespace SESport.AI.Jobs;

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
