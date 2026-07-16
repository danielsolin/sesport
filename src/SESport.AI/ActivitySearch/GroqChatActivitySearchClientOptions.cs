namespace SESport.AI.ActivitySearch;

public sealed record GroqChatActivitySearchClientOptions(
   Uri BaseAddress,
   string Model,
   string? ApiKey = null
);
