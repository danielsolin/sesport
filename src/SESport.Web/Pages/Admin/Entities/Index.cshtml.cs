using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Entities;

public class IndexModel(AdminRepository repository) : PageModel
{
   public IReadOnlyList<EntityListItem> Entities { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Entities = await repository.GetEntitiesAsync(cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteEntityAsync(id, cancellationToken);
      return RedirectToPage("./Index");
   }
}
