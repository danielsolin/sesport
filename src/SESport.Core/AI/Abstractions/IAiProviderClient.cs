using SESport.Core.AI.Models;

namespace SESport.Core.AI.Abstractions;

public interface IAiProviderClient
{
   string Kind { get; }

   Task<AiJobResult> GenerateAsync(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      string renderedPrompt,
      string inputPayloadJson,
      CancellationToken cancellationToken
   );
}
