using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Account;

public sealed class VerifyModel(
   MemberAuthService memberAuthService,
   ILogger<VerifyModel> logger
) : PageModel
{
   [BindProperty(SupportsGet = true)]
   public string? Token { get; set; }

   [BindProperty(SupportsGet = true)]
   public string? ReturnUrl { get; set; }

   public string? ErrorMessage { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      CancellationToken cancellationToken
   )
   {
      try
      {
         var member = await memberAuthService.ConsumeLoginTokenAsync(
            Token,
            cancellationToken
         );

         if(member is null)
         {
            ErrorMessage =
               "Länken är ogiltig, för gammal eller redan använd.";
            return Page();
         }

         var claims = new[]
         {
            new Claim(
               MemberClaimTypes.MemberId,
               member.Id.ToString()
            ),
            new Claim(
               ClaimTypes.NameIdentifier,
               member.Id.ToString()
            ),
            new Claim(ClaimTypes.Name, member.Email),
            new Claim(ClaimTypes.Email, member.Email)
         };
         var identity = new ClaimsIdentity(
            claims,
            MemberAuthenticationDefaults.Scheme
         );
         var principal = new ClaimsPrincipal(identity);
         await HttpContext.SignInAsync(
            MemberAuthenticationDefaults.Scheme,
            principal,
            new AuthenticationProperties
            {
               IsPersistent = true
            }
         );

         return LocalRedirect(NormalizeReturnUrl(ReturnUrl) ?? "/");
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         logger.LogError(
            exception,
            "Could not verify a member login link."
         );
         ErrorMessage =
            "Länken kunde inte behandlas just nu. Försök igen senare.";
         return Page();
      }
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
