namespace SESport.AI.Models;

public sealed record AiJobResult(
   Guid RunId,
   string JobId,
   string ProviderId,
   string Prompt,
   string OutputText,
   string? RawResponseJson,
   string? ErrorMessage
);
