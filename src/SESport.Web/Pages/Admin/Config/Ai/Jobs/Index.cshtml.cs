using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.AI;
using SESport.Data.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Jobs;

public class IndexModel(AiAdminRepository repository) : PageModel
{
   public IReadOnlyList<AiJobListItem> Jobs { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Jobs = await repository.GetJobsAsync(cancellationToken);
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
      await repository.DeleteJobAsync(id, cancellationToken);
      return RedirectToPage("./Index");
   }
}
