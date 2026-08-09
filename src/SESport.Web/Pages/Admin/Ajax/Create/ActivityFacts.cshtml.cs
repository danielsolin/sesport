using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Ajax.Create;

public sealed class ActivityFactsModel(ActivityEditPageService editService)
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
            error = "Title is required before finding facts."
         });
      }

      if(Activity.Id is null)
      {
         return BadRequest(new
         {
            error = "Save the activity before queueing a facts job."
         });
      }

      var savedActivity = await editService.LoadActivityAsync(
         Activity.Id.Value,
         cancellationToken
      );

      if(savedActivity is null)
      {
         return NotFound(new
         {
            error = "Activity not found."
         });
      }

      if(savedActivity.ActivityGroupId is null)
      {
         return BadRequest(new
         {
            error = "Assign the activity to an ActivityGroup before " +
               "finding group facts."
         });
      }

      var runId = await editService.QueueFactsAsync(
         savedActivity,
         cancellationToken
      );

      return new JsonResult(new
      {
         runId,
         status = "queued"
      });
   }
}
