namespace SESport.Core.AIActivitySearch;

public sealed record OpenAiResponsesActivitySearchClientOptions(
   Uri BaseAddress,
   string Model,
   string? ApiKey = null
);
