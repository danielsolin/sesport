using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Admin;

public class LoginModel(AdminLoginOptions adminOptions) : PageModel
{
   [BindProperty]
   public string Password { get; set; } = string.Empty;

   public string? ErrorMessage { get; private set; }

   public void OnGet()
   {
   }

   public async Task<IActionResult> OnPostAsync()
   {
      var configuredPassword = adminOptions.Password;

      if(string.IsNullOrWhiteSpace(configuredPassword))
      {
         ErrorMessage = "Admin password is not configured.";
         return Page();
      }

      if(!PasswordsMatch(Password, configuredPassword))
      {
         ErrorMessage = "Invalid password.";
         return Page();
      }

      var claims = new[]
      {
         new Claim(ClaimTypes.Name, "admin")
      };
      var identity = new ClaimsIdentity(
         claims,
         CookieAuthenticationDefaults.AuthenticationScheme
      );
      var principal = new ClaimsPrincipal(identity);

      await HttpContext.SignInAsync(
         CookieAuthenticationDefaults.AuthenticationScheme,
         principal
      );

      return RedirectToPage("/Admin/Dashboard/Index");
   }

   private static bool PasswordsMatch(string provided, string configured)
   {
      var providedHash = SHA256.HashData(
         Encoding.UTF8.GetBytes(provided)
      );
      var configuredHash = SHA256.HashData(
         Encoding.UTF8.GetBytes(configured)
      );

      return CryptographicOperations.FixedTimeEquals(
         providedHash,
         configuredHash
      );
   }
}
