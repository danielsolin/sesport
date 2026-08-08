using SESport.AI.Jobs;
using SESport.Core.AI;

namespace SESport.Web.Services;

public interface IAiAutomationService
{
   Task HandleActivityCreatedAsync(
      Guid activityId,
      CancellationToken cancellationToken
   );

   Task HandlePersonCreatedAsync(
      Guid personEntityId,
      CancellationToken cancellationToken
   );
}

public sealed class AiAutomationService(
   AiAutomationRepository repository,
   ActivityRepository activityRepository,
   ActivityAiInputBuilder inputBuilder,
   AiJobEligibilityService eligibilityService,
   PersonFactsService personFactsService,
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

      var eligibleJobIds = new List<string>();
      foreach(var jobId in jobIds)
      {
         if(await eligibilityService.CanQueueAsync(
               jobId,
               activity,
               cancellationToken
            ))
         {
            eligibleJobIds.Add(jobId);
            continue;
         }

         logger.LogInformation(
            "Skipping AI automation job {JobId} for activity {ActivityId}; "
               + "the sport does not require participant start times.",
            jobId,
            activityId
         );
      }

      if(eligibleJobIds.Count == 0)
      {
         return;
      }

      var input = await inputBuilder.BuildAsync(
         activity,
         cancellationToken,
         activity.ActivityGroupTitle
      );

      foreach(var jobId in eligibleJobIds)
      {
         await jobRunner.QueueAsync(
            new AiJobRequest(jobId, input, activityId.ToString()),
            cancellationToken
         );
      }
   }

   public async Task HandlePersonCreatedAsync(
      Guid personEntityId,
      CancellationToken cancellationToken
   )
   {
      var jobIds = await repository.GetEnabledJobIdsAsync(
         AiAutomationEventIds.PersonCreated,
         cancellationToken
      );

      if(!jobIds.Any(jobId => string.Equals(
            jobId,
            AiJobIds.FindPersonData,
            StringComparison.Ordinal
         )))
      {
         return;
      }

      await personFactsService.QueueAsync(
         personEntityId,
         cancellationToken
      );
   }
}
