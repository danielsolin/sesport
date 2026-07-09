using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Data;

namespace SESport.Web.Pages.Admin.Sources;

public class IndexModel(AdminRepository repository) : PageModel
{
   public IReadOnlyList<SourceListItem> Sources { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Sources = await repository.GetSourcesAsync(cancellationToken);
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteSourceAsync(id, cancellationToken);
      return RedirectToPage("./Index");
   }
}
