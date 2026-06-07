namespace SESport.AI.Models;

public sealed record AiPromptDefinition(
   Guid Id,
   string JobId,
   int Version,
   string SystemPrompt,
   string UserPromptTemplate,
   string? OutputSchemaJson,
   decimal? Temperature,
   int? MaxOutputTokens,
   bool Enabled
);
