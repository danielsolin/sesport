using SESport.AI.Models;

namespace SESport.AI.Abstractions;

public interface IAiPromptRenderer
{
   AiRenderedPrompt Render(
      AiPromptDefinition prompt,
      string inputPayloadJson
   );
}
