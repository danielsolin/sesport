using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin;

public class IndexModel(AdminRepository repository) : PageModel
{
   public IReadOnlyList<AdminArea> Areas { get; private set; } = [];

   public void OnGet()
   {
      Areas = repository.GetAdminAreas();
   }
}
