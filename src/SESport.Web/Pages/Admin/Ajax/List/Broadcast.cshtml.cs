using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Ajax.List;

public sealed class BroadcastModel(
   AdminBroadcastRepository repository,
   BroadcastParticipationService participationService
) : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      if(id == Guid.Empty)
      {
         return BadRequest(new { error = "Broadcast ID is required." });
      }

      var broadcast = await repository.GetByIdAsync(id, cancellationToken);

      if(broadcast is null)
      {
         return NotFound(new { error = "Broadcast not found." });
      }

      var broadcasts = await participationService.ApplyParticipationChecksAsync(
         [broadcast],
         cancellationToken
      );
      var refreshedBroadcast = broadcasts[0];

      return new JsonResult(new
      {
         broadcast = new
         {
            id = refreshedBroadcast.Id.ToString(),
            timeText = refreshedBroadcast.TimeText,
            timeOnlyText = refreshedBroadcast.TimeOnlyText,
            channelName = refreshedBroadcast.ChannelName,
            title = refreshedBroadcast.Title,
            description = refreshedBroadcast.Description,
            categories = refreshedBroadcast.Categories,
            isReplay = refreshedBroadcast.IsReplay,
            originalAirDate = refreshedBroadcast.OriginalAirDate?.ToString(
               "yyyy-MM-dd"
            ),
            isHidden = refreshedBroadcast.IsHidden,
            organizationEntityId =
               refreshedBroadcast.OrganizationEntityId?.ToString(),
            organizationEntityName = refreshedBroadcast.OrganizationEntityName,
            activityGroupSourceKindId =
               refreshedBroadcast.ActivityGroupSourceKindId,
            activityGroupSourceActivityId =
               refreshedBroadcast.ActivityGroupSourceActivityId?.ToString(),
            groupText = refreshedBroadcast.GroupText,
            participationStatusId =
               refreshedBroadcast.ParticipationCheck?.StatusId
         }
      });
   }
}
