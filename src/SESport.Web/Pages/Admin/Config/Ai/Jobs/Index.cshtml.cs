using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.AI.Models;
using SESport.AI.Persistence;

namespace SESport.Web.Pages.Admin.Config.Ai.Jobs;

public class IndexModel(AiAdminRepository repository) : PageModel
{
   public IReadOnlyList<AiJobListItem> Jobs { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Jobs = await repository.GetJobsAsync(cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }
}
