using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Members;

namespace SESport.Web.Pages.Account;

public sealed class LogoutModel : PageModel
{
   public async Task<IActionResult> OnPostAsync(string? returnUrl)
   {
      await HttpContext.SignOutAsync(MemberAuthenticationDefaults.Scheme);

      return LocalRedirect(NormalizeReturnUrl(returnUrl) ?? "/");
   }

   private static string? NormalizeReturnUrl(string? returnUrl)
   {
      return string.IsNullOrWhiteSpace(returnUrl) ||
         !returnUrl.StartsWith("/", StringComparison.Ordinal) ||
         returnUrl.StartsWith("//", StringComparison.Ordinal) ||
         returnUrl.Contains('\\')
         ? null
         : returnUrl;
   }
}
