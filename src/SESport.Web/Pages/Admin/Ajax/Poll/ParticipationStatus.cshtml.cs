using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Admin.Ajax.Poll;

public sealed class ParticipationStatusModel(
   BroadcastParticipationService participationService
) : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      List<Guid> broadcastIds,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var normalizedBroadcastIds = NormalizeBroadcastIds(broadcastIds);

         if(normalizedBroadcastIds.Count == 0)
         {
            return BadRequest(new
            {
               error = "Select at least one broadcast."
            });
         }

         var results = await participationService
            .GetParticipationCheckResultsAsync(
               normalizedBroadcastIds,
               cancellationToken
            );

         return new JsonResult(new
         {
            results
         });
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         return this.UnexpectedJsonError(exception);
      }
   }

   private static List<Guid> NormalizeBroadcastIds(
      IEnumerable<Guid> ids
   )
   {
      return ids
         .Where(id => id != Guid.Empty)
         .Distinct()
         .ToList();
   }
}
