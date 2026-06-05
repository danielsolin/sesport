using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.AI.Models;
using SESport.Data.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Providers;

public class IndexModel(AiAdminRepository repository) : PageModel
{
   public IReadOnlyList<AiProviderListItem> Providers { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Providers = await repository.GetProvidersAsync(cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }
}
