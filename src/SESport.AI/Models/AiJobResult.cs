namespace SESport.AI.Models;

public sealed record AiJobResult(
   Guid RunId,
   string JobId,
   string ProviderId,
   string? ProviderModel,
   string Prompt,
   string RawRequestJson,
   string OutputText,
   string? RawResponseJson,
   string? ErrorMessage
);
