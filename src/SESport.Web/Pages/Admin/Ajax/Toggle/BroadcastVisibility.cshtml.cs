using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Ajax.Toggle;

public sealed class BroadcastVisibilityModel(AdminBroadcastRepository repository)
   : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      bool isHidden,
      CancellationToken cancellationToken,
      [FromForm(Name = RouteKeys.Date)] DateOnly? date,
      [FromForm(Name = RouteKeys.SortColumn)] string? sortColumn,
      [FromForm(Name = RouteKeys.SortAsc)] bool sortAsc = true,
      [FromForm(Name = RouteKeys.ShowHidden)] bool showHidden = false,
      [FromForm(Name = RouteKeys.HideReplays)] bool hideReplays = false,
      [FromForm(Name = "SelectedSports")] List<string>? selectedSports = null
   )
   {
      if(id == Guid.Empty)
      {
         return BadRequest(new
         {
            error = "Broadcast ID is required."
         });
      }

      if(isHidden)
      {
         await repository.ShowAsync(id, cancellationToken);
      }
      else
      {
         await repository.HideAsync(id, cancellationToken);
      }

      if(!Request.Headers.Accept.ToString().Contains(
         "application/json",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         var routeValues = new Dictionary<string, object?>
         {
            [RouteKeys.Date] = DateDisplay.Format(date),
            [RouteKeys.SortColumn] = sortColumn,
            [RouteKeys.SortAsc] = sortAsc,
            [RouteKeys.ShowHidden] = showHidden
         };

         if(hideReplays)
         {
            routeValues[RouteKeys.HideReplays] = true;
         }

         if(selectedSports is not null)
         {
            var filteredSports = selectedSports
               .Where(value => !string.IsNullOrWhiteSpace(value))
               .Select(value => value.Trim())
               .ToArray();

            if(filteredSports.Length > 0)
            {
               routeValues["SelectedSports"] = filteredSports;
            }
         }

         return RedirectToPage("/Admin/Broadcasts/Index", routeValues);
      }

      return new JsonResult(new { hidden = !isHidden });
   }
}
