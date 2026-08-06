using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Account;

public sealed class LoginModel(
   MemberAuthService memberAuthService,
   ILogger<LoginModel> logger
) : PageModel
{
   [BindProperty]
   [Required(ErrorMessage = "Ange en e-postadress.")]
   [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
   public string Email { get; set; } = string.Empty;

   [BindProperty(SupportsGet = true)]
   public string? ReturnUrl { get; set; }

   public bool LinkRequested { get; private set; }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      ReturnUrl = NormalizeReturnUrl(ReturnUrl);

      if(!ModelState.IsValid ||
         MemberEmailNormalizer.Normalize(Email) is null)
      {
         if(ModelState.IsValid)
         {
            ModelState.AddModelError(
               nameof(Email),
               "Ange en giltig e-postadress."
            );
         }

         return Page();
      }

      try
      {
         await memberAuthService.RequestLoginLinkAsync(
            Email,
            ReturnUrl,
            cancellationToken
         );
         LinkRequested = true;
         return Page();
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         logger.LogError(
            exception,
            "Could not send a member login link."
         );
         ModelState.AddModelError(
            string.Empty,
            "Det gick inte att skicka länken just nu. Försök igen senare."
         );
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
