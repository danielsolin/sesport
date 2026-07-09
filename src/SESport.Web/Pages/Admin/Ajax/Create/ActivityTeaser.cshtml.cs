using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Ajax.Create;

public sealed class ActivityTeaserModel(ActivityEditPageService editService)
   : PageModel
{
   [BindProperty]
   public ActivityEditModel Activity { get; set; } = new();

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(Activity.Title))
      {
         return BadRequest(new
         {
            error = "Title is required before generating a teaser."
         });
      }

      if(Activity.Id is null)
      {
         return BadRequest(new
         {
            error = "Save the activity before queueing a teaser job."
         });
      }

      var runId = await editService.QueueTeaserAsync(
         Activity,
         cancellationToken
      );

      return new JsonResult(new
      {
         runId,
         status = "queued"
      });
   }
}
