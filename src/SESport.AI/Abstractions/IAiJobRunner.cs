using SESport.AI.Models;

namespace SESport.AI.Abstractions;

public interface IAiJobRunner
{
   Task<AiJobResult> RunAsync(
      AiJobRequest request,
      CancellationToken cancellationToken
   );
}
