using System.Text.Json;

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
      var inputParticipants = ReadInputParticipants(run.InputPayloadJson);
      var resultValues = BuildResultValues(
         currentParticipants,
         inputParticipants,
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
         IReadOnlyList<ParticipantInputDraft> inputParticipants,
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

      for(var index = 0; index < output.Participants.Count; index++)
      {
         var participant = output.Participants[index];

         if(!TryResolveEntityId(
               index,
               participant.Name,
               inputParticipants,
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
      int index,
      string outputName,
      IReadOnlyList<ParticipantInputDraft> inputParticipants,
      IReadOnlyDictionary<string, Guid> nameLookup,
      IReadOnlyDictionary<string, Guid> aliasLookup,
      IEnumerable<KeyValuePair<string, Guid>> allLookups,
      out Guid entityId
   )
   {
      if(index < inputParticipants.Count &&
         inputParticipants[index].EntityId is Guid inputEntityId)
      {
         entityId = inputEntityId;
         return true;
      }

      string?[] candidateNames =
      [
         index < inputParticipants.Count ? inputParticipants[index].Name :
            null,
         index < inputParticipants.Count ? inputParticipants[index].AliasName
            : null,
         outputName
      ];

      foreach(var candidateName in candidateNames)
      {
         if(TryMatchName(candidateName, nameLookup, aliasLookup, allLookups,
               out entityId))
         {
            return true;
         }
      }

      entityId = default;
      return false;
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

   private static IReadOnlyList<ParticipantInputDraft> ReadInputParticipants(
      string inputPayloadJson
   )
   {
      try
      {
         using var document = JsonDocument.Parse(inputPayloadJson);
         var root = document.RootElement;
         var names = ReadInputParticipantNames(root);
         var entities = ReadInputParticipantEntities(root);
         var count = Math.Max(names.Count, entities.Count);
         var result = new List<ParticipantInputDraft>(count);

         for(var index = 0; index < count; index++)
         {
            var entity = index < entities.Count ? entities[index] : null;
            result.Add(
               new ParticipantInputDraft(
                  entity?.EntityId,
                  entity?.Name ?? (index < names.Count ? names[index] : null),
                  entity?.AliasName
               )
            );
         }

         return result;
      }
      catch(JsonException)
      {
         return [];
      }
   }

   private static IReadOnlyList<string> ReadInputParticipantNames(
      JsonElement root
   )
   {
      if(!TryGetProperty(root, "participants", out var participants))
      {
         return [];
      }

      if(participants.ValueKind == JsonValueKind.String)
      {
         var value = participants.GetString();
         if(string.IsNullOrWhiteSpace(value))
         {
            return [];
         }

         return value
            .Split(
               ['\r', '\n'],
               StringSplitOptions.RemoveEmptyEntries |
                  StringSplitOptions.TrimEntries
            )
            .Select(StripPromptListItem)
            .Select(value => NormalizeParticipantName(value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
      }

      if(participants.ValueKind != JsonValueKind.Array)
      {
         return [];
      }

      var names = new List<string>();
      foreach(var participant in participants.EnumerateArray())
      {
         if(participant.ValueKind != JsonValueKind.String)
         {
            continue;
         }

         var value = NormalizeParticipantName(
            StripPromptListItem(participant.GetString())
         );
         if(!string.IsNullOrWhiteSpace(value))
         {
            names.Add(value);
         }
      }

      return names;
   }

   private static IReadOnlyList<ParticipantInputRecord>
      ReadInputParticipantEntities(JsonElement root)
   {
      if(!TryGetProperty(root, "participant_entities", out var entities) ||
         entities.ValueKind != JsonValueKind.Array)
      {
         return [];
      }

      var result = new List<ParticipantInputRecord>();
      foreach(var entity in entities.EnumerateArray())
      {
         if(entity.ValueKind != JsonValueKind.Object)
         {
            continue;
         }

         Guid? entityId = null;
         if(TryGetStringProperty(entity, "id", out var idText) &&
            Guid.TryParse(idText, out var parsedEntityId))
         {
            entityId = parsedEntityId;
         }

         result.Add(
            new ParticipantInputRecord(
               entityId,
               ReadNullableString(entity, "name"),
               ReadNullableString(entity, "alias_name")
            )
         );
      }

      return result;
   }

   private static bool TryGetProperty(
      JsonElement element,
      string propertyName,
      out JsonElement value
   )
   {
      if(element.TryGetProperty(propertyName, out value))
      {
         return true;
      }

      foreach(var property in element.EnumerateObject())
      {
         if(ComparePropertyNames(property.Name, propertyName))
         {
            value = property.Value;
            return true;
         }
      }

      value = default;
      return false;
   }

   private static bool TryGetStringProperty(
      JsonElement element,
      string propertyName,
      out string? value
   )
   {
      if(TryGetProperty(element, propertyName, out var property) &&
         property.ValueKind == JsonValueKind.String)
      {
         value = property.GetString()?.Trim();
         return true;
      }

      value = null;
      return false;
   }

   private static string? ReadNullableString(
      JsonElement element,
      string propertyName
   )
   {
      if(!TryGetStringProperty(element, propertyName, out var value))
      {
         return null;
      }

      return string.IsNullOrWhiteSpace(value) ? null : value;
   }

   private static string? NormalizeParticipantName(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      return BroadcastParticipantNameFormatter.Format(value).Trim();
   }

   private static string? StripPromptListItem(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      var trimmed = value.Trim();

      if(trimmed.StartsWith('-'))
      {
         trimmed = trimmed[1..].Trim();
      }

      if(trimmed.StartsWith('•'))
      {
         trimmed = trimmed[1..].Trim();
      }

      return trimmed;
   }

   private static bool ComparePropertyNames(string left, string right)
   {
      return string.Equals(
         left,
         right,
         StringComparison.OrdinalIgnoreCase
      ) ||
      NormalizePropertyName(left) == NormalizePropertyName(right);
   }

   private static string NormalizePropertyName(string value)
   {
      var builder = new System.Text.StringBuilder(value.Length);

      foreach(var character in value)
      {
         if(char.IsLetterOrDigit(character))
         {
            builder.Append(char.ToLowerInvariant(character));
         }
      }

      return builder.ToString();
   }

   private sealed record ParticipantInputDraft(
      Guid? EntityId,
      string? Name,
      string? AliasName
   );

   private sealed record ParticipantInputRecord(
      Guid? EntityId,
      string? Name,
      string? AliasName
   );
}
