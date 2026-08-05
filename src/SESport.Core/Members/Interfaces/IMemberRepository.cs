namespace SESport.Core.Members.Interfaces;

public interface IMemberRepository
{
   Task<bool> TryCreateLoginTokenAsync(
      string email,
      string normalizedEmail,
      string tokenHash,
      DateTimeOffset requestedAt,
      DateTimeOffset expiresAt,
      DateTimeOffset cooldownThreshold,
      DateTimeOffset windowStart,
      int maxRequestsPerWindow,
      CancellationToken cancellationToken
   );

   Task<Member?> ConsumeLoginTokenAsync(
      string tokenHash,
      DateTimeOffset consumedAt,
      CancellationToken cancellationToken
   );

   Task InvalidateLoginTokenAsync(
      string tokenHash,
      DateTimeOffset invalidatedAt,
      CancellationToken cancellationToken
   );
}
