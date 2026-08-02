using SESport.Core.AI;

namespace SESport.AI.Jobs;

public interface IAiPromptRenderer
{
   AiRenderedPrompt Render(
      AiPromptDefinition prompt,
      string inputPayloadJson
   );
}
