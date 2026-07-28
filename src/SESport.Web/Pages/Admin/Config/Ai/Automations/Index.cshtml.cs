using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.AI;
using SESport.Data.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Automations;

public class IndexModel(AiAdminRepository repository) : PageModel
{
   public IReadOnlyList<AiAutomationRuleListItem> Rules
   { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Rules = await repository.GetAutomationRulesAsync(
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteAutomationRuleAsync(id, cancellationToken);
      return RedirectToPage("./Index");
   }
}
