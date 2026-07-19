namespace SESport.Core.AI;

public sealed record AiJobDefinition(
   string Id,
   string Label,
   string? Description,
   string ProviderId,
   string OutputMode,
   string? ToolsJson,
   string? ConditionalToolsJson,
   int? ToolCallMaxTokens,
   bool RequiresWebSearch,
   bool Enabled,
   Guid? ActivePromptId,
   string? Model = null
);
