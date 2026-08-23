using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Extensions;
using SESport.Web.Pages.Admin.Broadcasts;

namespace SESport.Web.Pages.Admin.Ajax.Poll;

public sealed class ParticipationStatusModel(
   BroadcastParticipationService participationService,
   AdminBroadcastRepository broadcastRepository
) : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      List<Guid> broadcastIds,
      CancellationToken cancellationToken,
      bool pending = false
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

         if(this.WantsHtmlResponse())
         {
            var resultByBroadcastId = results.ToDictionary(
               result => result.Id
            );
            var partialResults = new List<BroadcastParticipationRunsViewModel>();

            foreach(var broadcastId in normalizedBroadcastIds)
            {
               var broadcast = await broadcastRepository.GetByIdAsync(
                  broadcastId,
                  cancellationToken
               );

               if(broadcast is null)
               {
                  continue;
               }

               resultByBroadcastId.TryGetValue(
                  broadcastId,
                  out var result
               );
               partialResults.Add(
                  CreateViewModel(
                     broadcast,
                     result,
                     pending
                  )
               );
            }

            return Partial(
               "_ParticipationStatusResults",
               partialResults
            );
         }

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

   private BroadcastParticipationRunsViewModel CreateViewModel(
      SESport.Data.Models.BroadcastListItem broadcast,
      BroadcastParticipationCheckResult? result,
      bool pending
   )
   {
      var activityRouteValues = new Dictionary<string, string?>
      {
         [$"{RouteKeys.BroadcastIds}[0]"] = broadcast.Id.ToString()
      };

      var returnUrl = BroadcastRowViewModel.GetActivityReturnUrl(Request);
      if(returnUrl is not null)
      {
         activityRouteValues[RouteKeys.ReturnUrl] = returnUrl;
      }

      return new BroadcastParticipationRunsViewModel(
         broadcast.Id,
         broadcast.OrganizationSportName,
         Url.Page("/Admin/Activities/Edit", activityRouteValues),
         Url.Page("/Admin/Ajax/Create/ParticipationCheck"),
         Url.Page("/Admin/Ajax/Update/RunField"),
         Url.Page("/Admin/Ajax/Create/ParticipantEntity"),
         ViewData["SearchUrl"] as string ?? string.Empty,
         result?.Checks ?? [],
         false,
         pending,
         false
      );
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
