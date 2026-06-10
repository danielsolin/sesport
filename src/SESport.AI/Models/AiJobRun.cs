namespace SESport.AI.Models;

public sealed record AiJobRun(
   Guid Id,
   string JobId,
   Guid PromptId,
   string ProviderId,
   AiJobRunStatus Status,
   string? CorrelationId,
   string InputPayloadJson,
   string RenderedPrompt,
   string RawRequestJson,
   string? RawResponseJson,
   string? OutputText,
   string? ErrorMessage,
   DateTimeOffset StartedAt,
   DateTimeOffset? CompletedAt,
   decimal? DurationSeconds,
   int? InputTokens,
   int? OutputTokens,
   int? ReasoningTokens
);
