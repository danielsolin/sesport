using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.AI;
using SESport.Web.Pages.Admin.Runs;
using TracePresenter =
   SESport.Web.Pages.Admin.Runs.AiRunToolTracePresenter;

namespace SESport.Web.Pages.Admin.Ajax.Poll;

public sealed class RunToolTraceModel(AiRepository repository) : PageModel
{
   public AiRunDetail Run { get; private set; } = null!;

   public IReadOnlyList<TracePresenter.ToolTraceTurnViewModel> Turns
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<TracePresenter.ToolTraceBadgeViewModel>
      SummaryBadges
   {
      get
      {
         return TracePresenter.BuildToolTraceSummaryBadges(Turns);
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
      Turns = TracePresenter.ParseToolTrace(run.ToolTraceJson);

      return Page();
   }
}
