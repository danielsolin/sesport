namespace SESport.Web.Services;

public sealed class MemberAuthService(
   IMemberRepository repository,
   IMemberEmailSender emailSender,
   MemberAuthOptions options
)
{
   public async Task RequestLoginLinkAsync(
      string email,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      var normalizedEmail = MemberEmailNormalizer.Normalize(email) ??
         throw new ArgumentException(
            "The email address is not valid.",
            nameof(email)
         );
      ValidateOptions();

      var now = DateTimeOffset.UtcNow;
      var rawToken = MemberLoginToken.Generate();
      var tokenHash = MemberLoginToken.Hash(rawToken);
      var created = await repository.TryCreateLoginTokenAsync(
         email.Trim(),
         normalizedEmail,
         tokenHash,
         now,
         now.Add(options.LoginTokenLifetime),
         now.Subtract(options.LoginRequestCooldown),
         now.Subtract(options.LoginRequestWindow),
         options.MaxLoginRequestsPerWindow,
         cancellationToken
      );

      if(!created)
      {
         return;
      }

      var loginLink = BuildLoginLink(rawToken, NormalizeReturnUrl(returnUrl));

      try
      {
         await emailSender.SendLoginLinkAsync(
            normalizedEmail,
            loginLink,
            options.LoginTokenLifetime,
            cancellationToken
         );
      }
      catch
      {
         await repository.InvalidateLoginTokenAsync(
            tokenHash,
            DateTimeOffset.UtcNow,
            CancellationToken.None
         );
         throw;
      }
   }

   public Task<Member?> ConsumeLoginTokenAsync(
      string? rawToken,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(rawToken))
      {
         return Task.FromResult<Member?>(null);
      }

      return ConsumeLoginTokenCoreAsync(rawToken, cancellationToken);
   }

   private async Task<Member?> ConsumeLoginTokenCoreAsync(
      string rawToken,
      CancellationToken cancellationToken
   )
   {
      return await repository.ConsumeLoginTokenAsync(
         MemberLoginToken.Hash(rawToken),
         DateTimeOffset.UtcNow,
         cancellationToken
      );
   }

   private string BuildLoginLink(string rawToken, string? returnUrl)
   {
      var baseUrl = options.PublicBaseUrl.TrimEnd('/');
      var link = baseUrl +
         "/Account/Verify?token=" +
         Uri.EscapeDataString(rawToken);

      return string.IsNullOrWhiteSpace(returnUrl)
         ? link
         : link + "&returnUrl=" + Uri.EscapeDataString(returnUrl);
   }

   private void ValidateOptions()
   {
      if(!Uri.TryCreate(
            options.PublicBaseUrl,
            UriKind.Absolute,
            out var publicBaseUri
         ) ||
         publicBaseUri.Scheme is not ("http" or "https"))
      {
         throw new InvalidOperationException(
            "MemberAuth:PublicBaseUrl must be an absolute HTTP URL."
         );
      }

      if(options.LoginTokenLifetime <= TimeSpan.Zero ||
         options.LoginRequestCooldown < TimeSpan.Zero ||
         options.LoginRequestWindow <= TimeSpan.Zero ||
         options.MaxLoginRequestsPerWindow < 1)
      {
         throw new InvalidOperationException(
            "Member authentication timing options are invalid."
         );
      }
   }

   private static string? NormalizeReturnUrl(string? returnUrl)
   {
      if(string.IsNullOrWhiteSpace(returnUrl) ||
         !returnUrl.StartsWith("/", StringComparison.Ordinal) ||
         returnUrl.StartsWith("//", StringComparison.Ordinal) ||
         returnUrl.Contains('\\'))
      {
         return null;
      }

      return returnUrl;
   }
}
