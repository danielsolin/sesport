namespace SESport.AI.Models;

public sealed record AiJobDefinition(
   string Id,
   string Label,
   string? Description,
   string ProviderId,
   string OutputMode,
   string? ToolsJson,
   bool RequiresWebSearch,
   bool Enabled,
   Guid? ActivePromptId
);
