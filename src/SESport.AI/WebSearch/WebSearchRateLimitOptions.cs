namespace SESport.AI.WebSearch;

public sealed record WebSearchRateLimitOptions
{
   public TimeSpan MinimumRequestInterval { get; init; } =
      TimeSpan.FromSeconds(5);

   public TimeSpan RateLimitedCooldown { get; init; } =
      TimeSpan.FromMinutes(10);

   public TimeSpan TransientFailureCooldown { get; init; } =
      TimeSpan.FromMinutes(1);
}
