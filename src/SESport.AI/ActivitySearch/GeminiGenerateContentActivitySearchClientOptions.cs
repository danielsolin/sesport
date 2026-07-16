namespace SESport.AI.ActivitySearch;

public sealed record GeminiGenerateContentActivitySearchClientOptions(
   Uri BaseAddress,
   string Model,
   string? ApiKey = null
);
