using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.AI.Models;
using SESport.Data.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Runs;

public class DetailsModel(AiRepository repository) : PageModel
{
   public AiRunDetail? Run { get; private set; }

   [BindProperty(SupportsGet = true)]
   public string? JobId { get; set; }

   [BindProperty(SupportsGet = true)]
   public string? StatusId { get; set; }

   public async Task<IActionResult> OnGetAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      Run = await repository.GetRunAsync(id, cancellationToken);

      return Run is null ? NotFound() : Page();
   }
}
