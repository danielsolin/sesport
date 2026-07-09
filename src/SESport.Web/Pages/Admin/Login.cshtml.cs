using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace SESport.Web.Pages.Admin;

public class LoginModel(IConfiguration configuration) : PageModel
{
   [BindProperty]
   public string Password { get; set; } = string.Empty;

   public string? ErrorMessage { get; private set; }

   public void OnGet()
   {
   }

   public async Task<IActionResult> OnPostAsync()
   {
      var configuredPassword = configuration["Admin:Password"];

      if(string.IsNullOrWhiteSpace(configuredPassword))
      {
         ErrorMessage = "Admin password is not configured.";
         return Page();
      }

      if(Password != configuredPassword)
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

      return RedirectToPage("/Admin/Broadcasts/Index");
   }
}
