namespace SESport.Core.Configuration;

public sealed record GeminiGenerateContentActivitySearchClientOptions(
   Uri BaseAddress,
   string Model,
   string? ApiKey = null
);
