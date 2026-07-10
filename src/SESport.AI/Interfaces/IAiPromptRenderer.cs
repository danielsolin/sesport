using SESport.Core.AI;

namespace SESport.AI.Interfaces;

public interface IAiPromptRenderer
{
   AiRenderedPrompt Render(
      AiPromptDefinition prompt,
      string inputPayloadJson
   );
}
