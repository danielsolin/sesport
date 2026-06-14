namespace SESport.AI.Models;

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

   public bool Enabled { get; set; } = true;
}

public sealed record AiJobListItem(
   string Id,
   string Label,
   string ProviderId,
   string OutputMode,
   bool Enabled
);

public sealed class AiJobEditModel
{
   public string? OriginalId { get; set; }

   public string Id { get; set; } = string.Empty;

   public string Label { get; set; } = string.Empty;

   public string? Description { get; set; }

   public string ProviderId { get; set; } = string.Empty;

   public string OutputMode { get; set; } = "text";

   public string? ActivePromptId { get; set; }

   public bool RequiresWebSearch { get; set; } = true;

   public bool Enabled { get; set; } = true;
}

public sealed record AiPromptListItem(
   string Id,
   string JobId,
   string JobLabel,
   int Version,
   bool Enabled
);

public sealed record AiRunListItem(
   Guid Id,
   string JobLabel,
   string ProviderLabel,
   string? ProviderModel,
   string StatusId,
   DateTimeOffset StartedAt,
   decimal? DurationSeconds
);

public sealed record AiRunDetail(
   Guid Id,
   string JobId,
   string JobLabel,
   Guid PromptId,
   int PromptVersion,
   string ProviderId,
   string ProviderLabel,
   string? ProviderModel,
   string StatusId,
   string? CorrelationId,
   string InputPayloadJson,
   string RenderedPrompt,
   string? RawRequestJson,
   string? RawResponseJson,
   string? ToolTraceJson,
   string? OutputText,
   string? ErrorMessage,
   DateTimeOffset StartedAt,
   DateTimeOffset? CompletedAt,
   decimal? DurationSeconds,
   int? InputTokens,
   int? OutputTokens,
   int? ReasoningTokens
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

   public bool Enabled { get; set; } = true;
}
