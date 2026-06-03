namespace SESport.Core.AIActivitySearch;

public sealed record GeminiGenerateContentActivitySearchClientOptions(
   Uri BaseAddress,
   string Model,
   string? ApiKey = null
);
