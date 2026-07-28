using System.Text.Json;

using SESport.AI.Interfaces;
using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Web.Services;

public sealed class BroadcastParticipationService(
   ActivityRepository activityRepository,
   AiRepository aiRepository,
   AdminRepository adminRepository,
   AdminBroadcastRepository broadcastRepository,
   IAiJobRunner aiJobRunner
)
{
   public async Task<BroadcastParticipationCheck?>
      GetParticipationCheckAsync(
         Guid broadcastId,
         Guid? runId,
         CancellationToken cancellationToken
      )
   {
      var checks = await aiRepository.GetParticipationCheckHistoryAsync(
         [broadcastId],
         cancellationToken
      );

      if(!checks.TryGetValue(broadcastId, out var participationChecks)
         || participationChecks.Count == 0)
      {
         return null;
      }

      if(runId is null || runId == Guid.Empty)
      {
         return participationChecks[0];
      }

      return participationChecks.FirstOrDefault(check =>
         check.RunId == runId.Value)
         ?? participationChecks[0];
   }

   public async Task RecordApplicationAsync(
      Guid runId,
      Guid activityId,
      IReadOnlyCollection<Guid> broadcastIds,
      CancellationToken cancellationToken
   )
   {
      var history = await aiRepository.GetParticipationCheckHistoryAsync(
         NormalizeBroadcastIds(broadcastIds),
         cancellationToken
      );
      var isParticipationRun = history.Values
         .SelectMany(checks => checks)
         .Any(check => check.RunId == runId);

      if(!isParticipationRun)
      {
         return;
      }

      await aiRepository.RecordApplicationAsync(
         runId,
         AiJobRunApplicationTargetTypes.Activity,
         activityId.ToString(),
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<BroadcastListItem>>
      ApplyParticipationChecksAsync(
         IReadOnlyList<BroadcastListItem> broadcasts,
         CancellationToken cancellationToken
      )
   {
      var broadcastIds = broadcasts.Select(broadcast => broadcast.Id).ToArray();
      var checkHistory = await aiRepository.GetParticipationCheckHistoryAsync(
         broadcastIds,
         cancellationToken
      );
      var activityGroupIds = broadcasts
         .Where(broadcast => broadcast.ActivityGroupId is not null)
         .Select(broadcast => broadcast.ActivityGroupId!.Value)
         .Distinct()
         .ToArray();
      var groupParticipants = await activityRepository
         .GetActivityGroupParticipantsAsync(
            activityGroupIds,
            cancellationToken
         );

      return broadcasts
         .Select(broadcast =>
         {
            checkHistory.TryGetValue(
               broadcast.Id,
               out var participationChecks
            );
            var participationCheck = participationChecks is null ||
               participationChecks.Count == 0
               ? null
               : participationChecks[0];
            var knownParticipants =
               broadcast.ActivityGroupId is not null &&
               groupParticipants.TryGetValue(
                  broadcast.ActivityGroupId.Value,
                  out var participants
               )
                  ? participants
                  : [];

            return broadcast with
            {
               ParticipationCheck = participationCheck,
               ParticipationChecks = participationChecks ?? [],
               ActivityGroupParticipants = knownParticipants
            };
         })
         .ToList();
   }

   public async Task QueueParticipationAsync(
      IReadOnlyCollection<Guid> broadcastIds,
      CancellationToken cancellationToken
   )
   {
      var normalizedBroadcastIds = NormalizeBroadcastIds(broadcastIds);

      if(normalizedBroadcastIds.Count == 0)
      {
         return;
      }

      var broadcasts = await broadcastRepository.GetActivitySourcesAsync(
         normalizedBroadcastIds,
         cancellationToken
      );
      var candidateOptionsCache =
         new Dictionary<Guid, IReadOnlyList<EntityOption>>();

      foreach(var broadcast in broadcasts)
      {
         var candidateOptions = await GetCandidateOptionsAsync(
            broadcast.EntityId,
            candidateOptionsCache,
            cancellationToken
         );

         await aiJobRunner.QueueAsync(
            new AiJobRequest(
               AiJobIds.DecidePrimaryCountryParticipation,
               CreateParticipationInputJson(
                  broadcast,
                  CreateCandidatesText(candidateOptions)
               ),
               broadcast.Id.ToString()
            ),
            cancellationToken
         );
      }
   }

   public async Task<IReadOnlyList<BroadcastParticipationCheckResult>>
      GetParticipationCheckResultsAsync(
         IReadOnlyCollection<Guid> broadcastIds,
         CancellationToken cancellationToken
      )
   {
      var normalizedBroadcastIds = NormalizeBroadcastIds(broadcastIds);

      if(normalizedBroadcastIds.Count == 0)
      {
         return [];
      }

      var broadcasts = await broadcastRepository.GetActivitySourcesAsync(
         normalizedBroadcastIds,
         cancellationToken
      );
      var checkHistory = await aiRepository.GetParticipationCheckHistoryAsync(
         normalizedBroadcastIds,
         cancellationToken
      );

      if(checkHistory.Count == 0)
      {
         return [];
      }

      var participantEntityIdsByOrgId =
         new Dictionary<Guid, IReadOnlyDictionary<string, Guid>>();
      var results = new List<BroadcastParticipationCheckResult>();

      foreach(var broadcast in broadcasts)
      {
         if(!checkHistory.TryGetValue(
            broadcast.Id,
            out var participationChecks
         ) ||
            participationChecks.Count == 0)
         {
            continue;
         }

         var participationCheck = participationChecks[0];
         var participantEntityIdsByName =
            await LoadParticipantEntityIdsAsync(
               broadcast.EntityId,
               participantEntityIdsByOrgId,
               cancellationToken
            );
         var displayChecks = participationChecks
            .Select(check => CreateParticipationCheckDisplay(
               check,
               participantEntityIdsByName
            ))
            .ToList();

         results.Add(
            new BroadcastParticipationCheckResult(
               broadcast.Id,
               participationCheck.RunId,
               participationCheck.StatusId,
               participationCheck.ToolRoundCount,
               broadcast.ChannelName,
               broadcast.Title,
               participationCheck.ErrorMessage,
               participationCheck.Participation,
               participationCheck.SourceUrls,
               displayChecks
            )
         );
      }

      return results;
   }

   private async Task<IReadOnlyDictionary<string, Guid>>
      LoadParticipantEntityIdsAsync(
         Guid? organizationEntityId,
         IDictionary<Guid, IReadOnlyDictionary<string, Guid>> cache,
         CancellationToken cancellationToken
      )
   {
      if(organizationEntityId is null)
      {
         return new Dictionary<string, Guid>();
      }

      if(cache.TryGetValue(organizationEntityId.Value, out var cached))
      {
         return cached;
      }

      var entityOptions =
         await adminRepository.GetParticipantEntityNameOptionsAsync(
            organizationEntityId.Value,
            cancellationToken
         );
      var entityIdsByName = BroadcastEntityFilter.CreateNameLookup(
         entityOptions,
         entity => entity.Name,
         entity => entity.Id
      );

      cache[organizationEntityId.Value] = entityIdsByName;
      return entityIdsByName;
   }

   private static BroadcastParticipationCheckDisplay
      CreateParticipationCheckDisplay(
         BroadcastParticipationCheck check,
         IReadOnlyDictionary<string, Guid> participantEntityIdsByName
      )
   {
      return new BroadcastParticipationCheckDisplay(
         check.RunId,
         check.StatusId,
         check.ToolRoundCount,
         check.Participation,
         GetParticipantDisplayItems(
            check.Participants,
            participantEntityIdsByName
         ),
         check.SourceUrls,
         check.ErrorMessage
      );
   }

   internal static IReadOnlyList<BroadcastParticipantDisplayItem>
      GetParticipantDisplayItems(
         IReadOnlyList<string> participantNames,
         IReadOnlyDictionary<string, Guid> participantEntityIdsByName
      )
   {
      Guid? templateEntityId = null;

      foreach(var participantName in participantNames)
      {
         if(BroadcastEntityFilter.TryGetNameMatch(
               participantEntityIdsByName,
               participantName,
               out var entityId
            ))
         {
            templateEntityId = entityId;
            break;
         }
      }

      return participantNames
         .Select(name =>
         {
            var displayName = BroadcastParticipantNameFormatter.Format(name);

            if(BroadcastEntityFilter.TryGetNameMatch(
                  participantEntityIdsByName,
                  name,
                  out var entityId
               ) ||
               BroadcastEntityFilter.TryGetFuzzyNameMatch(
                  participantEntityIdsByName,
                  name,
                  out entityId
               ))
            {
               return new BroadcastParticipantDisplayItem(
                  displayName,
                  $"/Admin/Entities/Edit/{entityId}",
                  null
               );
            }

            return new BroadcastParticipantDisplayItem(
               displayName,
               null,
               templateEntityId
            );
         })
         .ToList();
   }

   private static string CreateParticipationInputJson(
      BroadcastActivitySource broadcast,
      string candidates
   )
   {
      var localDate = DateOnly.FromDateTime(
         TimeZoneHelper.ToLocal(
            broadcast.StartsAt,
            SportDay.TimeZoneId
         ).Date
      );

      return JsonSerializer.Serialize(
         new
         {
            sport = NormalizeParticipationSports(broadcast.Categories),
            event_name = broadcast.Title,
            description = broadcast.Description,
            date = DateDisplay.Format(localDate),
            candidates
         }
      );
   }

   private static IReadOnlyList<string> NormalizeParticipationSports(
      IEnumerable<string> categories
   )
   {
      return categories
         .SelectMany(category => category.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries |
               StringSplitOptions.TrimEntries
         ))
         .Where(category => !string.IsNullOrWhiteSpace(category))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private async Task<IReadOnlyList<EntityOption>> GetCandidateOptionsAsync(
      Guid? organizationEntityId,
      IDictionary<Guid, IReadOnlyList<EntityOption>> cache,
      CancellationToken cancellationToken
   )
   {
      if(organizationEntityId is null)
      {
         return [];
      }

      if(cache.TryGetValue(organizationEntityId.Value, out var candidates))
      {
         return candidates;
      }

      candidates = await activityRepository
         .GetPersonEntitiesForPromptCandidatesAsync(
            organizationEntityId.Value,
            cancellationToken
         );
      cache[organizationEntityId.Value] = candidates;
      return candidates;
   }

   private static string CreateCandidatesText(
      IReadOnlyList<EntityOption> candidates
   )
   {
      return candidates.Count == 0
         ? string.Empty
         : string.Join(
            Environment.NewLine,
            candidates.Select(candidate => $"  - {candidate.Name}")
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

public sealed record BroadcastParticipationCheckResult(
   Guid Id,
   Guid RunId,
   string StatusId,
   int ToolRoundCount,
   string ChannelName,
   string Title,
   string? Error,
   string? Participation,
   IReadOnlyList<string> SourceUrls,
   IReadOnlyList<BroadcastParticipationCheckDisplay> Checks
);

public sealed record BroadcastParticipationCheckDisplay(
   Guid RunId,
   string StatusId,
   int ToolRoundCount,
   string? Participation,
   IReadOnlyList<BroadcastParticipantDisplayItem> Participants,
   IReadOnlyList<string> SourceUrls,
   string? ErrorMessage
)
{
   public bool HasResult => !string.IsNullOrWhiteSpace(Participation);

   public string BadgeText => HasResult
      ? Participation ?? StatusId
      : StatusId;

   public string ParticipantsPreview =>
      string.Join(", ", Participants.Select(participant => participant.Name));

   public string SummaryText =>
      !string.IsNullOrWhiteSpace(ErrorMessage)
         ? ErrorMessage
         : HasResult
            ? (Participants.Count == 0
               ? BadgeText
               : $"{BadgeText}: {ParticipantsPreview}")
            : StatusId;
}

public sealed record BroadcastParticipantDisplayItem(
   string Name,
   string? EditUrl,
   Guid? TemplateEntityId
);
