using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace SESport.Web.Services;

public sealed class SmtpEmailSender(
   SmtpEmailOptions options,
   IHostEnvironment environment,
   ILogger<SmtpEmailSender> logger
) : IMemberEmailSender
{
   public async Task SendLoginLinkAsync(
      string recipientEmail,
      string loginLink,
      TimeSpan tokenLifetime,
      CancellationToken cancellationToken
   )
   {
      if(!options.IsConfigured)
      {
         if(!environment.IsDevelopment())
         {
            throw new InvalidOperationException(
               "SMTP email delivery is not configured."
            );
         }

         logger.LogWarning(
            "SMTP is not configured. Development login link for " +
            "{Email}: {LoginLink}",
            recipientEmail,
            loginLink
         );
         return;
      }

      var fromAddress = string.IsNullOrWhiteSpace(options.FromName)
         ? new MailAddress(options.FromAddress)
         : new MailAddress(options.FromAddress, options.FromName);

      using var message = new MailMessage
      {
         From = fromAddress,
         Subject = "Logga in på sesport",
         Body = CreateHtmlBody(loginLink, tokenLifetime),
         BodyEncoding = Encoding.UTF8,
         IsBodyHtml = true
      };
      message.AlternateViews.Add(
         AlternateView.CreateAlternateViewFromString(
            CreatePlainTextBody(loginLink, tokenLifetime),
            Encoding.UTF8,
            MediaTypeNames.Text.Plain
         )
      );
      message.To.Add(new MailAddress(recipientEmail));

      using var client = new SmtpClient(options.Host, options.Port)
      {
         EnableSsl = options.UseSsl,
         UseDefaultCredentials = false
      };
      if(!string.IsNullOrWhiteSpace(options.Username))
      {
         client.Credentials = new NetworkCredential(
            options.Username,
            options.Password
         );
      }

      await client.SendMailAsync(message, cancellationToken);
   }

   private static string CreateHtmlBody(
      string loginLink,
      TimeSpan tokenLifetime
   )
   {
      var safeLoginLink = WebUtility.HtmlEncode(loginLink);
      var lifetimeMinutes = Math.Max(
         1,
         (int)Math.Ceiling(tokenLifetime.TotalMinutes)
      );

      return $"""
         <p>Du har begärt en inloggningslänk till sesport.</p>
         <p>
            <a href="{safeLoginLink}">Logga in på sesport</a>
         </p>
         <p>Länken gäller i {lifetimeMinutes} minuter och kan bara användas
         en gång.</p>
         <p>Om du inte begärde länken kan du ignorera detta meddelande.</p>
         """;
   }

   private static string CreatePlainTextBody(
      string loginLink,
      TimeSpan tokenLifetime
   )
   {
      var lifetimeMinutes = Math.Max(
         1,
         (int)Math.Ceiling(tokenLifetime.TotalMinutes)
      );

      return $"""
         Du har begärt en inloggningslänk till sesport.

         Logga in på sesport:
         {loginLink}

         Länken gäller i {lifetimeMinutes} minuter och kan bara användas
         en gång.

         Om du inte begärde länken kan du ignorera detta meddelande.
         """;
   }
}
