using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.AI.Models;
using SESport.AI.Persistence;
using SESport.Core.Formatting;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Runs;

public class IndexModel(
   AiAdminRepository adminRepository,
   AiRepository repository
) : PageModel
{
   public IReadOnlyList<AiRunListItem> Runs { get; private set; } = [];

   public IReadOnlyList<AiJobListItem> Jobs { get; private set; } = [];

   public IReadOnlyList<SelectListItem> StatusOptions { get; private set; } =
      [];

   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true)]
   public string? JobId { get; set; }

   [BindProperty(SupportsGet = true)]
   public string? StatusId { get; set; }

   public string DateText => DateDisplay.Format(SelectedDate);

   public DateOnly SelectedDate { get; private set; }

   public string? LoadError { get; private set; }

   private DateOnly GetSelectedDate()
   {
      return Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
   }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         SelectedDate = GetSelectedDate();
         Jobs = await adminRepository.GetJobsAsync(cancellationToken);
         StatusOptions =
         [
            new SelectListItem("All statuses", string.Empty),
            new SelectListItem("Pending", "pending"),
            new SelectListItem("Running", "running"),
            new SelectListItem("Completed", "completed"),
            new SelectListItem("Failed", "failed")
         ];
         Runs = await repository.GetRunsAsync(
            SelectedDate,
            JobId,
            StatusId,
            cancellationToken
         );
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
      SelectedDate = GetSelectedDate();
      await repository.DeleteRunAsync(id, cancellationToken);

      return RedirectToPage(
         "./Index",
         new
         {
            date = DateText,
            JobId,
            StatusId
         }
      );
   }
}
