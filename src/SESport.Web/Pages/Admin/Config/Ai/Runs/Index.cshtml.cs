using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.AI.Models;
using SESport.AI.Persistence;

namespace SESport.Web.Pages.Admin.Config.Ai.Runs;

public class IndexModel(
   AiAdminRepository adminRepository,
   AiRepository repository
) : PageModel
{
   public IReadOnlyList<AiRunListItem> Runs { get; private set; } = [];

   public IReadOnlyList<AiJobListItem> Jobs { get; private set; } = [];

   public IReadOnlyList<SelectListItem> StatusOptions { get; private set; } =
      [];

   [BindProperty(SupportsGet = true)]
   public string? JobId { get; set; }

   [BindProperty(SupportsGet = true)]
   public string? StatusId { get; set; }

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
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
}
