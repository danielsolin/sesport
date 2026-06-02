using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class IndexModel(ActivityRepository repository) : PageModel
{
   public IReadOnlyList<ActivityListItem> Activities { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Activities = await repository.GetDraftsAsync(cancellationToken);
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
      await repository.DeleteAsync(id, cancellationToken);
      return RedirectToPage("./Index");
   }
}
