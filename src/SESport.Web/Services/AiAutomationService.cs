using SESport.AI.Jobs;
using SESport.Core.AI;

namespace SESport.Web.Services;

public interface IAiAutomationService
{
   Task HandleActivityCreatedAsync(
      Guid activityId,
      CancellationToken cancellationToken
   );

   Task HandleActivityGroupCreatedAsync(
      Guid activityGroupId,
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
   IAiJobRunRepository runRepository,
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

      foreach(var jobId in jobIds)
      {
         var targetType = AiJobIds.GetTargetType(jobId);
         if(targetType != AiJobTargetType.Activity)
         {
            logger.LogInformation(
               "Skipping AI automation job {JobId} for " +
                  "activity {ActivityId}; " +
                  "the job does not target an activity.",
               jobId,
               activityId
            );
            continue;
         }

         if(!await eligibilityService.CanQueueAsync(
               jobId,
               activity,
               cancellationToken
            ))
         {
            logger.LogInformation(
               "Skipping AI automation job {JobId} for activity " +
                  "{ActivityId}; the sport does not require participant " +
                  "start times.",
               jobId,
               activityId
            );
            continue;
         }

         var promptTitle = jobId == AiJobIds.FindParticipantsStart
            ? null
            : activity.ActivityGroupTitle;
         await jobRunner.QueueAsync(
            new AiJobRequest(
               jobId,
               await inputBuilder.BuildAsync(
                  activity,
                  cancellationToken,
                  promptTitle
               ),
               activityId.ToString()
            ),
            cancellationToken
         );
      }
   }

   public async Task HandleActivityGroupCreatedAsync(
      Guid activityGroupId,
      CancellationToken cancellationToken
   )
   {
      var jobIds = await repository.GetEnabledJobIdsAsync(
         AiAutomationEventIds.ActivityGroupCreated,
         cancellationToken
      );

      if(jobIds.Count == 0)
      {
         return;
      }

      foreach(var jobId in jobIds)
      {
         if(AiJobIds.GetTargetType(jobId) != AiJobTargetType.ActivityGroup)
         {
            logger.LogInformation(
               "Skipping AI automation job {JobId} for ActivityGroup " +
                  "{ActivityGroupId}; the job does not target an " +
                  "activity group.",
               jobId,
               activityGroupId
            );
            continue;
         }

         var correlationId = activityGroupId.ToString();
         var existingRunId = await runRepository.GetExistingRunIdAsync(
            jobId,
            correlationId,
            cancellationToken
         );

         if(existingRunId is not null)
         {
            logger.LogInformation(
               "Skipping AI automation job {JobId} for ActivityGroup " +
                  "{ActivityGroupId}; a run already exists.",
               jobId,
               activityGroupId
            );
            continue;
         }

         await jobRunner.QueueAsync(
            new AiJobRequest(
               jobId,
               await inputBuilder.BuildActivityGroupAsync(
                  activityGroupId,
                  cancellationToken
               ),
               correlationId
            ),
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
