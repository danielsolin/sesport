namespace SESport.Core.AIActivityTeasers;

public sealed record ActivityTeaserGeneratorOptions(
   Uri BaseAddress,
   string Model,
   string? ApiKey = null
);
