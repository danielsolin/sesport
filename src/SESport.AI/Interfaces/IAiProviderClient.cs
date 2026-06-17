using System.Text.Json.Nodes;
using SESport.AI.Models;

namespace SESport.AI.Interfaces;

public interface IAiProviderClient
{
   string Kind { get; }

   JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   );

   Task<AiJobResult> GenerateAsync(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt,
      string inputPayloadJson,
      CancellationToken cancellationToken,
      Func<string?, CancellationToken, Task>? toolTraceUpdated = null
   );
}
