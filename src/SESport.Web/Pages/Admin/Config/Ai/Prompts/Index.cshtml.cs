using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.AI;
using System.Globalization;

namespace SESport.Web.Pages.Admin.Config.Ai.Prompts;

public class IndexModel(AiAdminRepository repository) : PageModel
{
   public IReadOnlyList<AiPromptListItem> CurrentPrompts
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<AiPromptListItem> UnusedPrompts
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         var prompts = await repository.GetPromptsAsync(cancellationToken);
         CurrentPrompts = prompts.Where(prompt => prompt.IsInUse).ToList();
         UnusedPrompts = prompts.Where(prompt => !prompt.IsInUse).ToList();
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

   public static string FormatNullableInt(int? value)
   {
      return value?.ToString(CultureInfo.InvariantCulture) ?? "-";
   }
}
