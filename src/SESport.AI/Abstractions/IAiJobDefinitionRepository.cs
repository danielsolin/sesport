using SESport.AI.Models;

namespace SESport.AI.Abstractions;

public interface IAiJobDefinitionRepository
{
   Task<AiJobDefinition?> GetJobAsync(
      string jobId,
      CancellationToken cancellationToken
   );

   Task<AiPromptDefinition?> GetActivePromptAsync(
      string jobId,
      CancellationToken cancellationToken
   );

   Task<AiProviderDefinition?> GetProviderAsync(
      string providerId,
      CancellationToken cancellationToken
   );
}
