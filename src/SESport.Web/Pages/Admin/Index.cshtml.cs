using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Admin;

public class IndexModel : PageModel
{
   public IActionResult OnGet()
   {
      return RedirectToPage("/Admin/Dashboard/Index");
   }
}
