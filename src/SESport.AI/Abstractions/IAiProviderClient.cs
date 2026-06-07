using SESport.AI.Models;

namespace SESport.AI.Abstractions;

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
