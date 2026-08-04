using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Ajax.Search;

public sealed class ActivityGroupModel(
   ActivityEditPageService editService
) : PageModel
{
   public async Task<IActionResult> OnGetAsync(
      string? term,
      string? sportId,
      Guid? organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      var results = await editService.SearchActivityGroupsAsync(
         term,
         sportId,
         cancellationToken,
         organizationEntityId
      );

      return new JsonResult(new
      {
         results = results.Select(group => new
         {
            id = group.Id,
            text = group.Label
         })
      });
   }
}
