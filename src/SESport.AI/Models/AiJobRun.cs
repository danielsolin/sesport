namespace SESport.AI.Models;

public sealed record AiJobRun(
   Guid Id,
   string JobId,
   Guid PromptId,
   string ProviderId,
   string? ProviderModel,
   AiJobRunStatus Status,
   string? CorrelationId,
   string InputPayloadJson,
   string RenderedPrompt,
   string RawRequestJson,
   string? RawResponseJson,
   string? ToolTraceJson,
   int ToolRoundCount,
   int ConversationCharacterCount,
   string? OutputText,
   string? ErrorMessage,
   DateTimeOffset StartedAt,
   DateTimeOffset? CompletedAt,
   decimal? DurationSeconds,
   int? InputTokens,
   int? OutputTokens,
   int? ReasoningTokens
);
