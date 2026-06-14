namespace SESport.AI.Models;

public sealed record AiJobDefinition(
   string Id,
   string Label,
   string? Description,
   string ProviderId,
   string OutputMode,
   bool RequiresWebSearch,
   bool Enabled,
   Guid? ActivePromptId
);
