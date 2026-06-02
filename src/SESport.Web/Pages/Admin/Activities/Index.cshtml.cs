using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class IndexModel(ActivityRepository repository) : PageModel
{
   [BindProperty(SupportsGet = true, Name = "status")]
   public string? Status { get; set; } = "Draft";

   public IReadOnlyList<ActivityListItem> Activities { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      Status = NormalizeStatus(Status) ?? string.Empty;

      try
      {
         Activities = Status switch
         {
            "Draft" => await repository.GetDraftsAsync(cancellationToken),
            "Published" => await repository.GetPublishedAsync(cancellationToken),
            _ => await repository.GetAllAsync(cancellationToken)
         };
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      Guid id,
      string? status,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteAsync(id, cancellationToken);
      return RedirectToPage("./Index", new { status = NormalizeStatus(status) });
   }

   private static string? NormalizeStatus(string? status)
   {
      return status is "Draft" or "Published" ? status : null;
   }
}
