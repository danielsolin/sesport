namespace SESport.Core.Configuration;

public sealed record WebSearchRateLimitOptions
{
   public TimeSpan MinimumRequestInterval { get; init; } =
      TimeSpan.FromSeconds(10);

   public TimeSpan RateLimitedCooldown { get; init; } =
      TimeSpan.FromMinutes(15);

   public TimeSpan TransientFailureCooldown { get; init; } =
      TimeSpan.FromMinutes(1);
}
