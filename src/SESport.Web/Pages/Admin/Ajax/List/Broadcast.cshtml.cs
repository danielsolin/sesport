using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Web.Pages.Admin.Broadcasts;

namespace SESport.Web.Pages.Admin.Ajax.List;

public sealed class BroadcastModel(
   AdminBroadcastRepository repository,
   BroadcastParticipationService participationService
) : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      CancellationToken cancellationToken,
      [FromForm(Name = RouteKeys.Date)] DateOnly? date,
      [FromForm(Name = RouteKeys.SortColumn)] string? sortColumn,
      [FromForm(Name = RouteKeys.SortAsc)] bool sortAsc = true,
      [FromForm(Name = RouteKeys.ShowHidden)] bool showHidden = false,
      [FromForm(Name = RouteKeys.HideReplays)] bool hideReplays = false,
      [FromForm(Name = RouteKeys.SelectedSports)]
         List<string>? selectedSports = null
   )
   {
      if(id == Guid.Empty)
      {
         return BadRequest(new { error = "Broadcast ID is required." });
      }

      var broadcast = await repository.GetByIdAsync(id, cancellationToken);

      if(broadcast is null)
      {
         return NotFound(new { error = "Broadcast not found." });
      }

      var broadcasts = await participationService.ApplyParticipationChecksAsync(
         [broadcast],
         cancellationToken
      );
      var refreshedBroadcast = broadcasts[0];

      return Partial(
         "/Pages/Admin/Broadcasts/_BroadcastRow.cshtml",
         BroadcastRowViewModel.Create(
            refreshedBroadcast,
            Url,
            Request,
            date,
            sortColumn,
            sortAsc,
            showHidden,
            hideReplays,
            selectedSports,
            ViewData["SearchUrl"] as string
         )
      );
   }
}
