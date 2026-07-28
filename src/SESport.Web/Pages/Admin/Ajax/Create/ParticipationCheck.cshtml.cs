using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Admin.Ajax.Create;

public sealed class ParticipationCheckModel(
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

         await participationService.QueueParticipationAsync(
            normalizedBroadcastIds,
            CancellationToken.None
         );

         return new JsonResult(new
         {
            queued = true,
            broadcastIds = normalizedBroadcastIds
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
