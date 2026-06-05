namespace SESport.Core.AI.Models;

public sealed record AiJobRequest(
   string JobId,
   string InputPayloadJson,
   string? CorrelationId = null
);
