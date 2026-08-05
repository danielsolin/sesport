namespace SESport.Core.Members.Interfaces;

public interface IMemberEmailSender
{
   Task SendLoginLinkAsync(
      string recipientEmail,
      string loginLink,
      TimeSpan tokenLifetime,
      CancellationToken cancellationToken
   );
}
