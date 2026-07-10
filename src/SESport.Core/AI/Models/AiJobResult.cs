namespace SESport.Core.AI;

public sealed record AiJobResult(
   Guid RunId,
   string JobId,
   string ProviderId,
   string? ProviderModel,
   string Prompt,
   string RawRequestJson,
   string OutputText,
   string? RawResponseJson,
   string? ToolTraceJson,
   int ToolRoundCount,
   int ConversationCharacterCount,
   int? InputTokens,
   int? OutputTokens,
   int? ReasoningTokens,
   string? ErrorMessage
);
