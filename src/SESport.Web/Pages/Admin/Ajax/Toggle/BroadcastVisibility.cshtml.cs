using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Data;

namespace SESport.Web.Pages.Admin.Ajax.Toggle;

public sealed class BroadcastVisibilityModel(BroadcastRepository repository)
   : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      bool isHidden,
      CancellationToken cancellationToken
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

      return new JsonResult(new { hidden = !isHidden });
   }
}
