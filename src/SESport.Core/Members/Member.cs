namespace SESport.Core.Members;

public sealed record Member(
   Guid Id,
   string Email,
   string NormalizedEmail,
   DateTimeOffset? EmailVerifiedAt,
   DateTimeOffset CreatedAt,
   DateTimeOffset UpdatedAt,
   DateTimeOffset? LastLoginAt
);
