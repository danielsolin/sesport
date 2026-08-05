using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace SESport.Core.Members;

public static class MemberEmailNormalizer
{
   private static readonly EmailAddressAttribute EmailAddressValidator =
      new();

   public static string? Normalize(string? email)
   {
      var trimmed = email?.Trim();

      if(string.IsNullOrWhiteSpace(trimmed) ||
         !EmailAddressValidator.IsValid(trimmed))
      {
         return null;
      }

      try
      {
         var address = new MailAddress(trimmed);
         return address.Address.Trim().ToLowerInvariant();
      }
      catch(FormatException)
      {
         return null;
      }
   }
}
