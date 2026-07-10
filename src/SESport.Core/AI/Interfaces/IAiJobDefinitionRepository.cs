namespace SESport.Core.AI;

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

   Task<AiPromptDefinition?> GetPromptAsync(
      Guid promptId,
      CancellationToken cancellationToken
   );

   Task<AiProviderDefinition?> GetProviderAsync(
      string providerId,
      CancellationToken cancellationToken
   );
}
