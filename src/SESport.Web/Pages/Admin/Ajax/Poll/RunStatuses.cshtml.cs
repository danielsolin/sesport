using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.AI;
using SESport.Web.Pages.Admin.Runs;

namespace SESport.Web.Pages.Admin.Ajax.Poll;

public sealed class RunStatusesModel(AiRepository repository) : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      List<Guid> runIds,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var normalizedRunIds = NormalizeRunIds(runIds);

         if(normalizedRunIds.Count == 0)
         {
            return BadRequest(new
            {
               error = "Select at least one run."
            });
         }

         var runs = await repository.GetRunsByIdsAsync(
            normalizedRunIds,
            cancellationToken
         );

         return new JsonResult(new
         {
            results = runs.Select(run => new
            {
               id = run.Id,
               statusId = run.StatusId,
               resultSummary = run.ResultSummary,
               maxPayloadChars = run.MaxPayloadCharacterCount,
               rounds = run.ToolRoundCount,
               duration = DetailsModel.FormatDuration(run)
            })
         });
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         return this.UnexpectedJsonError(exception);
      }
   }

   private static List<Guid> NormalizeRunIds(IEnumerable<Guid> ids)
   {
      return ids
         .Where(id => id != Guid.Empty)
         .Distinct()
         .ToList();
   }
}
