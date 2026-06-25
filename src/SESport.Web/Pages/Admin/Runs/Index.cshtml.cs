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
   AiRepository repository,
   RunDatePreferenceStore datePreferenceStore
) : PageModel
{
   public IReadOnlyList<AiRunListItem> Runs { get; private set; } = [];

   public IReadOnlyList<AiJobListItem> Jobs { get; private set; } = [];

   public IReadOnlyList<string> ExecutionEnvironmentValues { get; private set; }
      = [];

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
      return datePreferenceStore.ResolveDate(HttpContext, Date);
   }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         SelectedDate = GetSelectedDate();
         Jobs = await adminRepository.GetJobsAsync(cancellationToken);
         ExecutionEnvironmentValues =
            await repository.GetExecutionEnvironmentOptionsAsync(
               cancellationToken
            );
         StatusOptions =
         [
            new SelectListItem("All statuses", string.Empty),
            new SelectListItem("Pending", "pending"),
            new SelectListItem("Running", "running"),
            new SelectListItem("Completed", "completed"),
            new SelectListItem("Failed", "failed"),
            new SelectListItem("Archived", "archived")
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

   public IReadOnlyList<SelectListItem> GetExecutionEnvironmentOptions(
      string? selectedExecutionEnvironment
   )
   {
      return DetailsModel.BuildExecutionEnvironmentOptions(
         ExecutionEnvironmentValues,
         selectedExecutionEnvironment,
         SESport.AI.ExecutionEnvironment.Current,
         includeUnsetOption: false
      );
   }
}
