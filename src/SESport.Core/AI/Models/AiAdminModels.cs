namespace SESport.Core.AI;

public sealed record AiProviderListItem(
   string Id,
   string Label,
   string Kind,
   string? BaseAddress,
   string? Model,
   bool Enabled
);

public sealed class AiProviderEditModel
{
   public string? OriginalId { get; set; }

   public string Id { get; set; } = string.Empty;

   public string Label { get; set; } = string.Empty;

   public string Kind { get; set; } = string.Empty;

   public string? BaseAddress { get; set; }

   public string? Model { get; set; }

   public string? ApiKeySource { get; set; }

   public string RequestOptionsJson { get; set; } = "{}";

   public string? ConditionalToolsJson { get; set; }

   public bool Enabled { get; set; } = true;
}

public sealed record AiJobListItem(
   string Id,
   string Label,
   string ProviderId,
   string ProviderKind,
   int QueuePriority,
   string OutputMode,
   int? ActivePromptVersion,
   bool Enabled
);

public sealed class AiJobEditModel
{
   public string? OriginalId { get; set; }

   public string Id { get; set; } = string.Empty;

   public string Label { get; set; } = string.Empty;

   public string? Description { get; set; }

   public string ProviderId { get; set; } = string.Empty;

   public string? Model { get; set; }

   public int QueuePriority { get; set; }

   public string OutputMode { get; set; } = AiOutputModeIds.Text;

   public string? ActivePromptId { get; set; }

   public string? ToolsJson { get; set; }

   public string? ConditionalToolsJson { get; set; }

   public int? ToolCallMaxTokens { get; set; }

   public bool RequiresWebSearch { get; set; } = true;

   public bool IncludeSocialMedia { get; set; }

   public bool Enabled { get; set; } = true;
}

public sealed record AiPromptListItem(
   string Id,
   string JobId,
   string JobLabel,
   int Version,
   string SystemPrompt,
   string UserPromptTemplate,
   decimal? Temperature,
   int? MaxOutputTokens,
   int? MaxToolRounds,
   int? MinToolRounds,
   bool Enabled,
   bool IsInUse
);

public sealed record AiRunListItem(
   Guid Id,
   string JobId,
   string? ExecutionEnvironment,
   string JobLabel,
   string? EventName,
   DateOnly? EventDate,
   string ProviderLabel,
   string? ProviderModel,
   string StatusId,
   int ToolRoundCount,
   int MaxPayloadCharacterCount,
   string? ResultSummary,
   DateTimeOffset StartedAt,
   decimal? DurationSeconds
);

public sealed record AiRunDetail(
   Guid Id,
   string JobId,
   string JobLabel,
   Guid PromptId,
   int PromptVersion,
   string SystemPrompt,
   string UserPromptTemplate,
   decimal? PromptTemperature,
   int? PromptMaxOutputTokens,
   int? PromptMaxToolRounds,
   int MaxOutputTokens,
   string? PromptOutputSchemaJson,
   string PromptRequestOptionsJson,
   string ProviderId,
   string ProviderLabel,
   string ProviderKind,
   string? ProviderBaseAddress,
   string? ProviderModel,
   string? ProviderApiKeySource,
   string ProviderRequestOptionsJson,
   string StatusId,
   string? CorrelationId,
   string InputPayloadJson,
   string? RenderedSystemPrompt,
   string RenderedPrompt,
   string? RawRequestJson,
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
   int? ReasoningTokens,
   string? ExecutionEnvironment,
   string JobOutputMode,
   bool JobRequiresWebSearch,
   string? JobToolsJson,
   string? JobConditionalToolsJson,
   int? JobToolCallMaxTokens,
   int? PromptMinToolRounds = null,
   string? PromptCodexReasoningEffort = null,
   bool JobIncludeSocialMedia = false,
   DateTimeOffset? DiagnosticPayloadPurgedAt = null
);

public sealed class AiPromptEditModel
{
   public string? OriginalId { get; set; }

   public string Id { get; set; } = string.Empty;

   public string JobId { get; set; } = string.Empty;

   public int Version { get; set; } = 1;

   public string SystemPrompt { get; set; } = string.Empty;

   public string UserPromptTemplate { get; set; } = string.Empty;

   public string? OutputSchemaJson { get; set; }

   public string RequestOptionsJson { get; set; } = "{}";

   public decimal? Temperature { get; set; }

   public int? MaxOutputTokens { get; set; }

   public int? MaxToolRounds { get; set; }

   public int? MinToolRounds { get; set; }

   public string? CodexReasoningEffort { get; set; } =
      CodexReasoningEfforts.Default;

   public bool Enabled { get; set; } = true;
}
