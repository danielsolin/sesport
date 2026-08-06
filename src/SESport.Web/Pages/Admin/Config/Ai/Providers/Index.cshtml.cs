using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Providers;

public class IndexModel(AiAdminRepository repository) : PageModel
{
   public IReadOnlyList<AiProviderListItem> Providers { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Providers = await repository.GetProvidersAsync(cancellationToken);
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteProviderAsync(id, cancellationToken);
      return RedirectToPage("./Index");
   }
}
