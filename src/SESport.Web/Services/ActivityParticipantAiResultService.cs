using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Web.Services;

public sealed class ActivityParticipantAiResultService(
   IAiJobRunRepository runRepository,
   ActivityRepository activityRepository,
   ActivityParticipantAiResultRepository resultRepository,
   ILogger<ActivityParticipantAiResultService> logger
)
{
   public async Task<bool> TryApplyRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      var run = await runRepository.GetRunAsync(runId, cancellationToken);

      if(run is null ||
         run.JobId != AiJobIds.FindParticipantsStart ||
         !string.Equals(
            run.StatusId,
            AiJobRunStatusIds.Completed,
            StringComparison.Ordinal
         ))
      {
         return false;
      }

      if(!Guid.TryParse(run.CorrelationId, out var activityId))
      {
         logger.LogWarning(
            "Participant result run {RunId} has invalid correlation id.",
            runId
         );
         return false;
      }

      var output = ActivityParticipantAiOutputParser.Parse(
         run.OutputText ?? string.Empty
      );

      if(output is null)
      {
         logger.LogWarning(
            "Participant result run {RunId} completed without valid JSON.",
            runId
         );
         return false;
      }

      var activity = await activityRepository.GetForEditAsync(
         activityId,
         cancellationToken
      );

      if(activity is null)
      {
         logger.LogWarning(
            "Participant result run {RunId} references missing activity.",
            runId
         );
         return false;
      }

      var currentParticipants = await activityRepository
         .GetParticipantsForEditAsync(
            activityId,
            [],
            cancellationToken
         );
      var resultValues = BuildResultValues(
         currentParticipants,
         output
      );
      var savedValueCount = await resultRepository.UpsertAsync(
         new ActivityParticipantAiResultDraft(
            activityId,
            run.JobId,
            runId,
            output.CheckedSources,
            resultValues
         ),
         cancellationToken
      );

      if(output.Participants.Count > 0 && savedValueCount == 0)
      {
         logger.LogWarning(
            "Participant result run {RunId} did not map any values.",
            runId
         );
         return false;
      }

      if(resultValues.Count > 0 && savedValueCount < resultValues.Count)
      {
         logger.LogWarning(
            "Participant result run {RunId} skipped {SkippedCount} " +
               "unmappable values.",
            runId,
            resultValues.Count - savedValueCount
         );
      }

      await runRepository.RecordApplicationAsync(
         runId,
         AiJobRunApplicationTargetTypes.Activity,
         activityId.ToString(),
         cancellationToken
      );

      return true;
   }

   private static IReadOnlyList<ActivityParticipantAiResultValueDraft>
      BuildResultValues(
         IReadOnlyList<ActivityParticipantListItem> currentParticipants,
         ActivityParticipantAiOutputDraft output
      )
   {
      var nameLookup = BroadcastEntityFilter.CreateNameLookup(
         currentParticipants,
         participant => participant.Name,
         participant => participant.Id
      );
      var aliasLookup = BroadcastEntityFilter.CreateNameLookup(
         currentParticipants.Where(participant =>
            !string.IsNullOrWhiteSpace(participant.Alias)
         ),
         participant => participant.Alias,
         participant => participant.Id
      );
      var allLookups = nameLookup.Concat(aliasLookup).ToList();
      var result = new List<ActivityParticipantAiResultValueDraft>();

      foreach(var participant in output.Participants)
      {
         if(!TryResolveEntityId(
               participant.Name,
               nameLookup,
               aliasLookup,
               allLookups,
               out var entityId
            ))
         {
            continue;
         }

         foreach(var field in participant.Fields)
         {
            result.Add(
               new ActivityParticipantAiResultValueDraft(
                  entityId,
                  field.FieldKey,
                  field.ValueText,
                  field.ValueJson,
                  participant.Sources
               )
            );
         }
      }

      return result;
   }

   private static bool TryResolveEntityId(
      string outputName,
      IReadOnlyDictionary<string, Guid> nameLookup,
      IReadOnlyDictionary<string, Guid> aliasLookup,
      IEnumerable<KeyValuePair<string, Guid>> allLookups,
      out Guid entityId
   )
   {
      return TryMatchName(
         outputName,
         nameLookup,
         aliasLookup,
         allLookups,
         out entityId
      );
   }

   private static bool TryMatchName(
      string? value,
      IReadOnlyDictionary<string, Guid> nameLookup,
      IReadOnlyDictionary<string, Guid> aliasLookup,
      IEnumerable<KeyValuePair<string, Guid>> allLookups,
      out Guid entityId
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         entityId = default;
         return false;
      }

      return BroadcastEntityFilter.TryGetNameMatch(
               nameLookup,
               value,
               out entityId
            ) ||
         BroadcastEntityFilter.TryGetNameMatch(
               aliasLookup,
               value,
               out entityId
            ) ||
         BroadcastEntityFilter.TryGetFuzzyNameMatch(
               allLookups,
               value,
               out entityId
            );
   }

}
