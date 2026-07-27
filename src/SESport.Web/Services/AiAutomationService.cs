using SESport.AI.Interfaces;
using SESport.Core.AI;
using SESport.Data;
using SESport.Data.AI;

namespace SESport.Web.Services;

public interface IAiAutomationService
{
   Task HandleActivityCreatedAsync(
      Guid activityId,
      CancellationToken cancellationToken
   );
}

public sealed class AiAutomationService(
   AiAutomationRepository repository,
   ActivityRepository activityRepository,
   ActivityAiInputBuilder inputBuilder,
   IAiJobRunner jobRunner,
   ILogger<AiAutomationService> logger
) : IAiAutomationService
{
   public async Task HandleActivityCreatedAsync(
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      var jobIds = await repository.GetEnabledJobIdsAsync(
         AiAutomationEventIds.ActivityCreated,
         cancellationToken
      );

      if(jobIds.Count == 0)
      {
         return;
      }

      var activity = await activityRepository.GetForEditAsync(
         activityId,
         cancellationToken
      );

      if(activity is null)
      {
         logger.LogWarning(
            "Activity {ActivityId} was not found for AI automation.",
            activityId
         );
         return;
      }

      var input = await inputBuilder.BuildAsync(
         activity,
         cancellationToken,
         activity.ActivityGroupTitle
      );

      foreach(var jobId in jobIds)
      {
         await jobRunner.QueueAsync(
            new AiJobRequest(jobId, input, activityId.ToString()),
            cancellationToken
         );
      }
   }
}
