using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Config.BroadcastIgnoreRules;

public class IndexModel(AdminRepository repository) : PageModel
{
   public IReadOnlyList<BroadcastIgnoreRuleListItem> Rules
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Rules = await repository.GetBroadcastIgnoreRulesAsync(
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
      string kind,
      string value,
      string? sourceKey,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteBroadcastIgnoreRuleAsync(
         kind,
         value,
         sourceKey,
         cancellationToken
      );

      return RedirectToPage();
   }
}
