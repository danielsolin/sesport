using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.AI.Models;
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
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }
}
