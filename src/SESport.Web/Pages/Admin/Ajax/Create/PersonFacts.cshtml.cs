using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Admin.Ajax.Create;

public sealed class PersonFactsModel(PersonFactsService factsService)
   : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      if(id == Guid.Empty)
      {
         return BadRequest(new { error = "Entity id is required." });
      }

      try
      {
         var runId = await factsService.QueueAsync(
            id,
            cancellationToken
         );

         if(runId is null)
         {
            return NotFound(new { error = "Entity not found." });
         }

         return new JsonResult(new
         {
            runId,
            status = "queued"
         });
      }
      catch(PersonFactsValidationException exception)
      {
         return BadRequest(new { error = exception.Message });
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         return this.UnexpectedJsonError(exception);
      }
   }
}
