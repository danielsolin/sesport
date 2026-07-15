namespace SESport.Core.Configuration;

public sealed record GroqChatActivitySearchClientOptions(
   Uri BaseAddress,
   string Model,
   string? ApiKey = null
);
