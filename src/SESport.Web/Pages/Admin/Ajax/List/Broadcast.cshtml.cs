using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Broadcast;
using SESport.Core.Formatting;
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
            timeOnlyText = TimeTextFormatter.FormatTimeOnlyText(
               refreshedBroadcast.TimeText
            ),
            channelName = refreshedBroadcast.ChannelName,
            title = refreshedBroadcast.Title,
            description = refreshedBroadcast.Description,
            categories = refreshedBroadcast.Categories,
            isReplay = refreshedBroadcast.IsReplay,
            originalAirDate = DateDisplay.Format(
               refreshedBroadcast.OriginalAirDate
            ),
            isHidden = refreshedBroadcast.IsHidden,
            organizationEntityId =
               refreshedBroadcast.OrganizationEntityId?.ToString(),
            organizationEntityName = refreshedBroadcast.OrganizationEntityName,
            groupValue = BroadcastListDisplayFormatter.FormatGroupValue(
               refreshedBroadcast.Title,
               refreshedBroadcast.ActivityGroupTitle,
               refreshedBroadcast.ActivityGroupDraftTitle
            ),
            activityGroupId =
               refreshedBroadcast.ActivityGroupId?.ToString(),
            activityGroupTitle = refreshedBroadcast.ActivityGroupTitle,
            activityGroupDraftTitle =
               refreshedBroadcast.ActivityGroupDraftTitle,
            activityGroupSourceKindId =
               refreshedBroadcast.ActivityGroupSourceKindId,
            activityGroupSourceActivityId =
               refreshedBroadcast.ActivityGroupSourceActivityId?.ToString(),
            groupText = BroadcastListDisplayFormatter.FormatGroupText(
               refreshedBroadcast.Title,
               refreshedBroadcast.ActivityGroupSourceKindId,
               refreshedBroadcast.ActivityGroupId,
               refreshedBroadcast.ActivityGroupTitle,
               refreshedBroadcast.ActivityGroupDraftTitle
            ),
            participationStatusId =
               refreshedBroadcast.ParticipationCheck?.StatusId
         }
      });
   }
}
