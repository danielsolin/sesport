namespace SESport.AI.Models;

public sealed record AiPromptDefinition(
   Guid Id,
   string JobId,
   int Version,
   string SystemPrompt,
   string UserPromptTemplate,
   string? OutputSchemaJson,
   string RequestOptionsJson,
   decimal? Temperature,
   int? MaxOutputTokens,
   int? MaxToolRounds,
   bool Enabled
);
