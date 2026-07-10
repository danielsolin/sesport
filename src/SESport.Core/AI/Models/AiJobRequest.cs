namespace SESport.Core.AI;

public sealed record AiJobRequest(
   string JobId,
   string InputPayloadJson,
   string? CorrelationId = null
);
