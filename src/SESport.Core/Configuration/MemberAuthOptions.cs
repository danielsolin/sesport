namespace SESport.Core.Configuration;

public sealed record MemberAuthOptions
{
   public string PublicBaseUrl { get; init; } = string.Empty;

   public TimeSpan LoginTokenLifetime { get; init; } =
      TimeSpan.FromMinutes(15);

   public TimeSpan LoginRequestCooldown { get; init; } =
      TimeSpan.FromMinutes(1);

   public TimeSpan LoginRequestWindow { get; init; } =
      TimeSpan.FromHours(1);

   public int MaxLoginRequestsPerWindow { get; init; } = 5;

   public TimeSpan MemberCookieLifetime { get; init; } =
      TimeSpan.FromDays(30);
}
