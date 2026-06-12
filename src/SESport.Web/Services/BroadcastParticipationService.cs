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

   public async Task<IReadOnlyList<BroadcastParticipationCheckResult>>
      CheckSwedishParticipationAsync(
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
      var results = new List<BroadcastParticipationCheckResult>();

      foreach(var broadcast in broadcasts)
      {
         var result = await aiJobRunner.RunAsync(
            new AiJobRequest(
               ParticipationJobId,
               CreateParticipationInputJson(broadcast),
               broadcast.Id.ToString()
            ),
            cancellationToken
         );
         var sourceUrls = ParticipationSourceUrlExtractor.Extract(
            result.RawResponseJson
         );

         if(!string.IsNullOrWhiteSpace(result.ErrorMessage))
         {
            results.Add(
               new BroadcastParticipationCheckResult(
                  broadcast.Id,
                  result.RunId,
                  broadcast.ChannelName,
                  broadcast.Title,
                  result.ErrorMessage,
                  null,
                  [],
                  [],
                  sourceUrls
               )
            );

            continue;
         }

         var parsed = ParseParticipationResult(result.OutputText);

         if(parsed is null)
         {
            results.Add(
               new BroadcastParticipationCheckResult(
                  broadcast.Id,
                  result.RunId,
                  broadcast.ChannelName,
                  broadcast.Title,
                  "The model returned invalid JSON.",
                  null,
                  [],
                  [],
                  sourceUrls
               )
            );

            continue;
         }

         var participantItems = await ResolveParticipantItemsAsync(
            parsed.SwedishParticipants,
            cancellationToken
         );

         results.Add(
            new BroadcastParticipationCheckResult(
               broadcast.Id,
               result.RunId,
               broadcast.ChannelName,
               broadcast.Title,
               null,
               parsed.SwedishParticipation,
               parsed.SwedishParticipants,
               participantItems,
               sourceUrls
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

   private static SwedishParticipationResult? ParseParticipationResult(
      string outputText
   )
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.ValueKind != JsonValueKind.Object)
         {
            return null;
         }

         if(
            !root.TryGetProperty(
               "SwedishParticipation",
               out var participation
            ) ||
            participation.ValueKind != JsonValueKind.String
         )
         {
            return null;
         }

         var participants = new List<string>();

         if(
            root.TryGetProperty(
               "SwedishParticipants",
               out var participantsElement
            ) &&
            participantsElement.ValueKind == JsonValueKind.Array
         )
         {
            foreach(var participant in participantsElement.EnumerateArray())
            {
               if(participant.ValueKind != JsonValueKind.String)
               {
                  continue;
               }

               var name = participant.GetString();

               if(!string.IsNullOrWhiteSpace(name))
               {
                  participants.Add(name);
               }
            }
         }

         return new SwedishParticipationResult(
            participation.GetString() ?? string.Empty,
            participants
         );
      }
      catch(JsonException)
      {
         return null;
      }
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
                  $"/Admin/Entities/Edit/{entityId}"
               )
            );
         }
         else
         {
            items.Add(new BroadcastParticipantDisplayItem(name, null));
         }
      }

      return items;
   }

   private sealed record SwedishParticipationResult(
      string SwedishParticipation,
      IReadOnlyList<string> SwedishParticipants
   );
}

public sealed record BroadcastParticipationCheckResult(
   Guid Id,
   Guid RunId,
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
   string? EditUrl
);
