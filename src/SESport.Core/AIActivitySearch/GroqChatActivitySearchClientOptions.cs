namespace SESport.Core.AIActivitySearch;

public sealed record GroqChatActivitySearchClientOptions(
   Uri BaseAddress,
   string Model,
   string? ApiKey = null
);
