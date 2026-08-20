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
      HttpRequest request,
      CancellationToken cancellationToken
   )
   {
      var normalizedEmail = MemberEmailNormalizer.Normalize(email) ??
         throw new ArgumentException(
            "The email address is not valid.",
            nameof(email)
         );
      ValidateOptions();
      ValidateRequestOrigin(request);

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

      var loginLink = BuildLoginLink(
         rawToken,
         request,
         NormalizeReturnUrl(returnUrl)
      );

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
      HttpRequest request,
      string? returnUrl
   )
   {
      var baseUrl = request.Scheme + "://" +
         request.Host.ToUriComponent();
      var link = baseUrl +
         "/Account/Verify?token=" +
         Uri.EscapeDataString(rawToken);

      return string.IsNullOrWhiteSpace(returnUrl)
         ? link
         : link + "&returnUrl=" + Uri.EscapeDataString(returnUrl);
   }

   private static void ValidateRequestOrigin(HttpRequest request)
   {
      if(request.Scheme is not ("http" or "https") ||
         string.IsNullOrWhiteSpace(request.Host.Host))
      {
         throw new InvalidOperationException(
            "The request must have an absolute HTTP origin."
         );
      }
   }

   private void ValidateOptions()
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
