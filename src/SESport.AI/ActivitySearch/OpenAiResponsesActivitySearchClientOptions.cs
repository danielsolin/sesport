namespace SESport.AI.ActivitySearch;

public sealed record OpenAiResponsesActivitySearchClientOptions(
   Uri BaseAddress,
   string Model,
   string? ApiKey = null,
   string WebSearchToolType = "web_search"
);
