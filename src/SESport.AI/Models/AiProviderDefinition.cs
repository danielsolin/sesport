namespace SESport.AI.Models;

public sealed record AiProviderDefinition(
   string Id,
   string Label,
   string Kind,
   string? BaseAddress,
   string? Model,
   string? ApiKeySource,
   string RequestOptionsJson,
   bool Enabled
);
