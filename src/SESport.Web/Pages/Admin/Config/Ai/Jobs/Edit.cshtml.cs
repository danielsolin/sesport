using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.AI.Models;
using SESport.Data.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Jobs;

public class EditModel(AiAdminRepository repository) : PageModel
{
   [BindProperty]
   public AiJobEditModel Job { get; set; } = new();

   public IReadOnlyList<AiProviderListItem> Providers { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string? id,
      CancellationToken cancellationToken
   )
   {
      await LoadProvidersAsync(cancellationToken);

      if (string.IsNullOrWhiteSpace(id))
      {
         return Page();
      }

      Job = await repository.GetJobForEditAsync(
         id,
         cancellationToken
      ) ?? new AiJobEditModel();

      return Job.OriginalId is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      await LoadProvidersAsync(cancellationToken);
      ValidateJob();

      if (!ModelState.IsValid)
      {
         return Page();
      }

      try
      {
         await repository.SaveJobAsync(Job, cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
         return Page();
      }

      return RedirectToPage("./Index");
   }

   private async Task LoadProvidersAsync(CancellationToken cancellationToken)
   {
      Providers = await repository.GetProvidersAsync(cancellationToken);
   }

   private void ValidateJob()
   {
      if (string.IsNullOrWhiteSpace(Job.Id))
      {
         ModelState.AddModelError("Job.Id", "ID is required.");
      }

      if (string.IsNullOrWhiteSpace(Job.Label))
      {
         ModelState.AddModelError("Job.Label", "Label is required.");
      }

      if (string.IsNullOrWhiteSpace(Job.ProviderId))
      {
         ModelState.AddModelError("Job.ProviderId", "Provider is required.");
      }

      if (string.IsNullOrWhiteSpace(Job.OutputMode))
      {
         ModelState.AddModelError("Job.OutputMode", "Output mode is required.");
      }

      if (!string.IsNullOrWhiteSpace(Job.OutputMode)
         && Job.OutputMode is not ("text" or "json_object"))
      {
         ModelState.AddModelError(
            "Job.OutputMode",
            "Output mode must be text or json_object."
         );
      }

   }
}
