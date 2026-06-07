using SESport.AI.Models;

namespace SESport.AI.Abstractions;

public interface IAiPromptRenderer
{
   string Render(AiPromptDefinition prompt, string inputPayloadJson);
}
