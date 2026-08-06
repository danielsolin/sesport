using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.AI;
using SESport.Web.Pages.Admin.Runs;

namespace SESport.Web.Pages.Admin.Ajax.Poll;

public sealed class RunToolTraceModel(AiRepository repository) : PageModel
{
   public AiRunDetail Run { get; private set; } = null!;

   public IReadOnlyList<DetailsModel.ToolTraceTurnViewModel> Turns
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<DetailsModel.ToolTraceBadgeViewModel> SummaryBadges
   {
      get
      {
         return DetailsModel.BuildToolTraceSummaryBadges(Turns);
      }
   }

   public async Task<IActionResult> OnGetAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      var run = await repository.GetRunAsync(id, cancellationToken);

      if(run is null)
      {
         return NotFound();
      }

      Run = run;
      Turns = DetailsModel.ParseToolTrace(run.ToolTraceJson);

      return Page();
   }
}
