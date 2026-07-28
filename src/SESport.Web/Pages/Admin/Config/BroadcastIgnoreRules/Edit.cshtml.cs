using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Web.Pages.Admin.Config.BroadcastIgnoreRules;

public class EditModel(AdminRepository repository) : PageModel
{
   [BindProperty]
   public BroadcastIgnoreRuleEditModel Rule { get; set; } = new();

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string? kind,
      string? value,
      string? sourceKey,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(kind) && string.IsNullOrWhiteSpace(value))
      {
         return Page();
      }

      Rule = await repository.GetBroadcastIgnoreRuleForEditAsync(
         kind ?? string.Empty,
         value ?? string.Empty,
         sourceKey,
         cancellationToken
      ) ?? new BroadcastIgnoreRuleEditModel();

      return Rule.OriginalKind is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      ValidateRule();

      if(!ModelState.IsValid)
      {
         return Page();
      }

      try
      {
         await repository.SaveBroadcastIgnoreRuleAsync(Rule, cancellationToken);
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         return Page();
      }

      return RedirectToPage("./Index");
   }

   private void ValidateRule()
   {
      if(string.IsNullOrWhiteSpace(Rule.Kind))
      {
         ModelState.AddModelError("Rule.Kind", "Kind is required.");
      }

      if(string.IsNullOrWhiteSpace(Rule.Value))
      {
         ModelState.AddModelError("Rule.Value", "Value is required.");
      }
   }
}
