using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.AI.Models;
using SESport.Data.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Runs;

public class IndexModel(AiRepository repository) : PageModel
{
   public IReadOnlyList<AiRunListItem> Runs { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Runs = await repository.GetRunsAsync(cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }
}
