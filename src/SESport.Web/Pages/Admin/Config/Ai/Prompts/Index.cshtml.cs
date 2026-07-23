using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.AI;
using SESport.Data.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Prompts;

public class IndexModel(AiAdminRepository repository) : PageModel
{
   public IReadOnlyList<AiPromptListItem> Prompts { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Prompts = await repository.GetPromptsAsync(cancellationToken);
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      await repository.DeletePromptAsync(id, cancellationToken);
      return RedirectToPage("./Index");
   }

   public static string Truncate(string value, int maxLength)
   {
      return value.Length <= maxLength
         ? value
         : value[..maxLength] + "...";
   }
}
