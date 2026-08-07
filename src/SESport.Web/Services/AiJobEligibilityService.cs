using SESport.Core.AI;
using SESport.Data.Models;

namespace SESport.Web.Services;

public sealed class AiJobEligibilityService(
   ActivityRepository activityRepository
)
{
   public async Task<bool> CanQueueAsync(
      string jobId,
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      if(jobId != AiJobIds.FindParticipantsStart)
      {
         return true;
      }

      return await activityRepository.RequiresParticipantStartTimesAsync(
         activity.SportId,
         cancellationToken
      );
   }
}
