using SESport.AI.Models;

namespace SESport.AI.Interfaces;

public interface IAiPromptRenderer
{
   AiRenderedPrompt Render(
      AiPromptDefinition prompt,
      string inputPayloadJson
   );
}
