using SESport.Core.AI.Models;

namespace SESport.Core.AI.Abstractions;

public interface IAiPromptRenderer
{
   string Render(AiPromptDefinition prompt, string inputPayloadJson);
}
