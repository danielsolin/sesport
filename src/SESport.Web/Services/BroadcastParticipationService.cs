using System.Text.Json;

using SESport.AI.Interfaces;
using SESport.AI.Models;
using SESport.AI.Persistence;
using SESport.Core.Broadcast;
using SESport.Data;

namespace SESport.Web.Services;

public sealed class BroadcastParticipationService(
   AdminRepository adminRepository,
   ActivityRepository activityRepository,
   AiRepository aiRepository,
   BroadcastRepository broadcastRepository,
   IAiJobRunner aiJobRunner
)
{
   private const string ParticipationJobId =
      "decide-swedish-participation";

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

            return broadcast with
            {
               ParticipationCheck = participationCheck,
               ParticipationChecks = participationChecks ?? []
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
      var candidateOptions = await activityRepository.GetEntityOptionsAsync(
         cancellationToken
      );

      foreach(var broadcast in broadcasts)
      {
         await aiJobRunner.QueueAsync(
            new AiJobRequest(
               ParticipationJobId,
               CreateParticipationInputJson(
                  broadcast,
                  BroadcastParticipationCandidateResolver.CreateCandidatesText(
                     broadcast,
                     candidateOptions
                  )
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

      var entityOptions = await adminRepository.GetPersonEntityNameOptionsAsync(
         cancellationToken
      );
      var entityByName = entityOptions
         .Where(entity => !string.IsNullOrWhiteSpace(entity.Name))
         .GroupBy(entity =>
            BroadcastEntityFilter.NormalizeName(entity.Name))
         .Where(group => !string.IsNullOrWhiteSpace(group.Key))
         .ToDictionary(group => group.Key, group => group.First().Id);
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
         var participantItems = ResolveParticipantItems(
            participationCheck.Participants,
            entityByName
         );

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
               participationCheck.Participants,
               participantItems,
               participationCheck.SourceUrls,
               participationChecks
            )
         );
      }

      return results;
   }

   private static string CreateParticipationInputJson(
      BroadcastActivitySource broadcast,
      string candidates
   )
   {
      var localDate = DateOnly.FromDateTime(
         BroadcastRepository.ToLocal(broadcast.StartsAt).Date
      );

      return JsonSerializer.Serialize(
         new
         {
            sport = broadcast.Categories,
            event_name = broadcast.Title,
            date = $"{localDate:yyyy-MM-dd}",
            candidates
         }
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

   private static IReadOnlyList<BroadcastParticipantDisplayItem>
      ResolveParticipantItems(
         IReadOnlyList<string> participantNames,
         IReadOnlyDictionary<string, Guid> entityByName
      )
   {
      if(participantNames.Count == 0)
      {
         return [];
      }

      Guid? templateEntityId = null;

      foreach(var name in participantNames)
      {
         var normalizedName = BroadcastEntityFilter.NormalizeName(name);

         if(!string.IsNullOrWhiteSpace(normalizedName) &&
            entityByName.TryGetValue(normalizedName, out var entityId))
         {
            templateEntityId = entityId;
            break;
         }
      }

      var items = new List<BroadcastParticipantDisplayItem>();

      foreach(var name in participantNames)
      {
         var normalizedName = BroadcastEntityFilter.NormalizeName(name);

         if(!string.IsNullOrWhiteSpace(normalizedName) &&
            entityByName.TryGetValue(normalizedName, out var entityId))
         {
            items.Add(
               new BroadcastParticipantDisplayItem(
                  name,
                  $"/Admin/Entities/Edit/{entityId}",
                  null
               )
            );
         }
         else
         {
            items.Add(
               new BroadcastParticipantDisplayItem(
                  name,
                  null,
                  templateEntityId
               )
            );
         }
      }

      return items;
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
   IReadOnlyList<string> Participants,
   IReadOnlyList<BroadcastParticipantDisplayItem> ParticipantItems,
   IReadOnlyList<string> SourceUrls,
   IReadOnlyList<BroadcastParticipationCheck> Checks
);

public sealed record BroadcastParticipantDisplayItem(
   string Name,
   string? EditUrl,
   Guid? TemplateEntityId
);
