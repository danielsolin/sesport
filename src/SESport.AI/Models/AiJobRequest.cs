namespace SESport.AI.Models;

public sealed record AiJobRequest(
   string JobId,
   string InputPayloadJson,
   string? CorrelationId = null
);
