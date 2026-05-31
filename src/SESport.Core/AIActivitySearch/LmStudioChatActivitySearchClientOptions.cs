namespace SESport.Core.AIActivitySearch;

public sealed record LmStudioChatActivitySearchClientOptions(
   Uri BaseAddress,
   string Model,
   string PluginId,
   IReadOnlyCollection<string> AllowedTools,
   string? ApiKey = null
);
