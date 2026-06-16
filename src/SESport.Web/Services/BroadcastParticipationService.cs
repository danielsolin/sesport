using System.Text.Json;
using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.AI.Persistence;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Data;

namespace SESport.Web.Services;

public sealed class BroadcastParticipationService(
   AdminRepository adminRepository,
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
         CancellationToken cancellationToken
      )
   {
      var checks = await aiRepository.GetParticipationChecksAsync(
         [broadcastId],
         cancellationToken
      );

      return checks.TryGetValue(broadcastId, out var participationCheck)
         ? participationCheck
         : null;
   }

   public async Task<IReadOnlyList<BroadcastListItem>>
      ApplyParticipationChecksAsync(
         IReadOnlyList<BroadcastListItem> broadcasts,
         CancellationToken cancellationToken
      )
   {
      var broadcastIds = broadcasts.Select(broadcast => broadcast.Id).ToArray();
      var checks = await aiRepository.GetParticipationChecksAsync(
         broadcastIds,
         cancellationToken
      );

      return broadcasts
         .Select(broadcast =>
         {
            checks.TryGetValue(broadcast.Id, out var participationCheck);

            return broadcast with
            {
               ParticipationCheck = participationCheck
            };
         })
         .ToList();
   }

   public async Task QueueSwedishParticipationAsync(
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

      foreach(var broadcast in broadcasts)
      {
         await aiJobRunner.QueueAsync(
            new AiJobRequest(
               ParticipationJobId,
               CreateParticipationInputJson(broadcast),
               broadcast.Id.ToString()
            ),
            cancellationToken
         );
      }
   }

   public async Task<IReadOnlyList<BroadcastParticipationCheckResult>>
      GetSwedishParticipationCheckResultsAsync(
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
      var checks = await aiRepository.GetParticipationChecksAsync(
         normalizedBroadcastIds,
         cancellationToken
      );
      var results = new List<BroadcastParticipationCheckResult>();

      foreach(var broadcast in broadcasts)
      {
         if(!checks.TryGetValue(broadcast.Id, out var participationCheck))
         {
            continue;
         }

         var participantItems = await ResolveParticipantItemsAsync(
            participationCheck.SwedishParticipants,
            cancellationToken
         );

         results.Add(
            new BroadcastParticipationCheckResult(
               broadcast.Id,
               participationCheck.RunId,
               participationCheck.StatusId,
               broadcast.ChannelName,
               broadcast.Title,
               participationCheck.ErrorMessage,
               participationCheck.SwedishParticipation,
               participationCheck.SwedishParticipants,
               participantItems,
               participationCheck.SourceUrls
            )
         );
      }

      return results;
   }

   private static string CreateParticipationInputJson(
      BroadcastActivitySource broadcast
   )
   {
      var localStart = BroadcastRepository.ToLocal(broadcast.StartsAt);

      return JsonSerializer.Serialize(
         new
         {
            sport = broadcast.Categories,
            event_name = broadcast.Title,
            date_time = $"{localStart:yyyy-MM-dd HH:mm}"
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

   private async Task<IReadOnlyList<BroadcastParticipantDisplayItem>>
      ResolveParticipantItemsAsync(
         IReadOnlyList<string> participantNames,
         CancellationToken cancellationToken
      )
   {
      if(participantNames.Count == 0)
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
   string ChannelName,
   string Title,
   string? Error,
   string? SwedishParticipation,
   IReadOnlyList<string> SwedishParticipants,
   IReadOnlyList<BroadcastParticipantDisplayItem> SwedishParticipantItems,
   IReadOnlyList<string> SourceUrls
);

public sealed record BroadcastParticipantDisplayItem(
   string Name,
   string? EditUrl,
   Guid? TemplateEntityId
);
