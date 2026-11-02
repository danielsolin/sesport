namespace SESport.Web.Services;

public sealed class MemberAuthService(
   IMemberRepository repository,
   IMemberEmailSender emailSender,
   MemberAuthOptions options
)
{
   public async Task RequestLoginLinkAsync(
      string email,
      CancellationToken cancellationToken
   )
   {
      var normalizedEmail = MemberEmailNormalizer.Normalize(email) ??
         throw new ArgumentException(
            "The email address is not valid.",
            nameof(email)
         );
      var publicBaseUri = ValidateOptions();

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

      var loginLink = BuildLoginLink(rawToken, publicBaseUri);

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

   private static string BuildLoginLink(
      string rawToken,
      Uri publicBaseUri
   )
   {
      var baseUrl = publicBaseUri.GetLeftPart(UriPartial.Authority);
      var link = baseUrl +
         "/Account/Verify?token=" +
         Uri.EscapeDataString(rawToken);

      return link;
   }

   private Uri ValidateOptions()
   {
      if(options.LoginTokenLifetime <= TimeSpan.Zero ||
         options.LoginRequestCooldown < TimeSpan.Zero ||
         options.LoginRequestWindow <= TimeSpan.Zero ||
         options.MaxLoginRequestsPerWindow < 1)
      {
         throw new InvalidOperationException(
            "Member authentication timing options are invalid."
         );
      }

      if(!Uri.TryCreate(
            options.PublicBaseUrl,
            UriKind.Absolute,
            out var publicBaseUri
         ) ||
         (publicBaseUri.Scheme != Uri.UriSchemeHttp &&
            publicBaseUri.Scheme != Uri.UriSchemeHttps) ||
         string.IsNullOrWhiteSpace(publicBaseUri.Host) ||
         !string.IsNullOrEmpty(publicBaseUri.UserInfo) ||
         publicBaseUri.AbsolutePath != "/" ||
         !string.IsNullOrEmpty(publicBaseUri.Query) ||
         !string.IsNullOrEmpty(publicBaseUri.Fragment))
      {
         throw new InvalidOperationException(
            "MemberAuth:PublicBaseUrl must be an absolute HTTP(S) origin."
         );
      }

      return publicBaseUri;
   }
}
