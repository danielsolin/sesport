using SESport.Core.AI.Models;

namespace SESport.Core.AI.Abstractions;

public interface IAiJobRunner
{
   Task<AiJobResult> RunAsync(
      AiJobRequest request,
      CancellationToken cancellationToken
   );
}
