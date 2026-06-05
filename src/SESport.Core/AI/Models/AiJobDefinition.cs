namespace SESport.Core.AI.Models;

public sealed record AiJobDefinition(
   string Id,
   string Label,
   string? Description,
   string ProviderId,
   string OutputMode,
   bool Enabled
);
