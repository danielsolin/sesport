using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Core.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Automations;

public class EditModel(AiAdminRepository repository) : PageModel
{
   [BindProperty]
   public AiAutomationRuleEditModel Rule { get; set; } = new();

   public IReadOnlyList<SelectListItem> Events { get; } =
   [
      new(
         "Activity created",
         AiAutomationEventIds.ActivityCreated
      ),
      new(
         "Person created",
         AiAutomationEventIds.PersonCreated
      )
   ];

   public IReadOnlyList<AiJobListItem> Jobs { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      Guid? id,
      CancellationToken cancellationToken
   )
   {
      await LoadJobsAsync(cancellationToken);

      if(id is null)
      {
         return Page();
      }

      Rule = await repository.GetAutomationRuleAsync(
         id.Value,
         cancellationToken
      ) ?? new AiAutomationRuleEditModel();
      return Rule.Id is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      await LoadJobsAsync(cancellationToken);

      if(!Events.Any(item => item.Value == Rule.EventId))
      {
         ModelState.AddModelError(
            "Rule.EventId",
            "Select a supported event."
         );
      }

      if(!Jobs.Any(job => job.Id == Rule.JobId))
      {
         ModelState.AddModelError("Rule.JobId", "Select an AI job.");
      }

      if(!ModelState.IsValid)
      {
         return Page();
      }

      try
      {
         await repository.SaveAutomationRuleAsync(
            Rule,
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         return Page();
      }

      return RedirectToPage("./Index");
   }

   private async Task LoadJobsAsync(CancellationToken cancellationToken)
   {
      Jobs = await repository.GetJobsAsync(cancellationToken);
   }
}
