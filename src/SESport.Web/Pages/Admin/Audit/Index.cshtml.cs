using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Audit;

public class IndexModel(AuditRepository repository) : PageModel
{
   public IReadOnlyList<AuditArea> Areas { get; private set; } = [];

   public void OnGet()
   {
      Areas = repository.GetAuditAreas();
   }
}
