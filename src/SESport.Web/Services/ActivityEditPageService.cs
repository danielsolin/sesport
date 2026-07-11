using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.AI.Interfaces;
using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data;
using System.Text.Json;

namespace SESport.Web.Services;

public sealed class ActivityEditPageService(
   ActivityRepository repository,
   AdminRepository adminRepository,
   AdminBroadcastRepository broadcastRepository,
   BroadcastParticipationService participationService,
   IAiJobRunner aiJobRunner
)
{
   public async Task<ActivityEditOptions> LoadOptionsAsync(
      IEnumerable<Guid> selectedEntityIds,
      Guid? organizationEntityId,
      CancellationToken cancellationToken,
      string? sportId = null
   )
   {
      try
      {
         var selectedIds = selectedEntityIds.ToHashSet();
         var entities = await GetEntityPickerOptionsAsync(
            selectedIds,
            organizationEntityId,
            cancellationToken
         );

         var entityOptions = entities
            .Select(entity => new SelectListItem
            {
               Value = entity.Id.ToString(),
               Text = FormatEntityLabel(entity),
               Selected = selectedIds.Contains(entity.Id)
            })
            .ToList();

         var organizationOptions = (
            await adminRepository.GetOrganizationEntityOptionsAsync(
               cancellationToken,
               sportId
            )
         )
            .Select(entity => new SelectListItem(
               $"{entity.Name} ({entity.Sport})",
               entity.Id.ToString()
            ))
            .ToList();

         var activityTypes = await repository.GetActivityTypeOptionsAsync(
            cancellationToken
         );
         var sports = await repository.GetSportOptionsAsync(cancellationToken);

         return new ActivityEditOptions(
            entityOptions,
            organizationOptions,
            activityTypes,
            sports,
            null
         );
      }
      catch(Exception exception)
      {
         return new ActivityEditOptions(
            [],
            [],
            [],
            [],
            exception.Message
         );
      }
   }

   public async Task<ActivityEditModel?> LoadActivityAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      return await repository.GetForEditAsync(id, cancellationToken);
   }

   public async Task<IReadOnlyList<ActivityParticipantListItem>>
      LoadParticipantsAsync(
         ActivityEditModel activity,
         CancellationToken cancellationToken
      )
   {
      return await repository.GetParticipantsForEditAsync(
         activity.Id,
         activity.LinkedEntityIds ?? [],
         cancellationToken
      );
   }

   public async Task DeleteParticipantAsync(
      Guid activityId,
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteParticipantAsync(
         activityId,
         entityId,
         cancellationToken
      );
   }

   public async Task AddParticipantAsync(
      Guid activityId,
      Guid entityId,
      Guid organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      await repository.AddParticipantAsync(
         activityId,
         entityId,
         organizationEntityId,
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<ActivityParticipantListItem>>
      SearchParticipantCandidatesAsync(
         Guid organizationEntityId,
         string term,
         IReadOnlyCollection<Guid> excludedEntityIds,
         CancellationToken cancellationToken
      )
   {
      return await repository.SearchParticipantCandidatesAsync(
         organizationEntityId,
         term,
         excludedEntityIds,
         cancellationToken
      );
   }

   public async Task SaveAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      _ = await repository.SaveAsync(activity, cancellationToken);
      await broadcastRepository.HideAsync(
         NormalizeBroadcastIds(activity.BroadcastIds),
         cancellationToken
      );
   }

   public async Task<Guid?> PrefillFromBroadcastsAsync(
      ActivityEditModel activity,
      IReadOnlyCollection<Guid> ids,
      Guid? participationRunId,
      CancellationToken cancellationToken
   )
   {
      var normalizedIds = NormalizeBroadcastIds(ids);

      if(normalizedIds.Count == 0)
      {
         return null;
      }

      var broadcasts = await broadcastRepository.GetActivitySourcesAsync(
         normalizedIds,
         cancellationToken
      );

      if(broadcasts.Count == 0)
      {
         return null;
      }

      var firstBroadcast = broadcasts[0];
      var localStart = AdminBroadcastRepository.ToLocal(
         firstBroadcast.StartsAt
      );
      var participationCheck =
         await participationService.GetParticipationCheckAsync(
            firstBroadcast.Id,
            participationRunId,
            cancellationToken
         );
      var selectableEntities = participationCheck is null
         ? []
         : await GetSelectableEntitiesAsync(
            firstBroadcast.EntityId,
            cancellationToken
         );

      activity.BroadcastIds = [firstBroadcast.Id];
      activity.OrganizationEntityId = firstBroadcast.EntityId;
      activity.TvChannelName = firstBroadcast.ChannelName;
      activity.Title = BroadcastActivityPrefillBuilder.CreateActivityTitle(
         firstBroadcast,
         selectableEntities,
         participationCheck
      );
      activity.Description = firstBroadcast.Description;
      activity.ActivityType =
         BroadcastActivityTypeResolver.ResolveActivityType(
            firstBroadcast.Title,
            firstBroadcast.Description,
            firstBroadcast.Categories
         )?.ToString() ?? ActivityType.Match.ToString();
      activity.IsPublished = true;
      activity.EvidenceComment = BroadcastActivityPrefillBuilder
         .CreateEvidenceComment(firstBroadcast, participationCheck);

      var sportId = BroadcastCategorySportIdResolver.ResolveSportId(
         broadcasts.SelectMany(broadcast => broadcast.Categories)
      );

      if(!string.IsNullOrWhiteSpace(sportId))
      {
         activity.SportId = sportId;
      }

      activity.ActivityDate = DateOnly.FromDateTime(localStart.DateTime);
      activity.LocalStartTime = TimeOnly.FromDateTime(localStart.DateTime);
      activity.TimeZoneId = SportDay.TimeZoneId;
      activity.EvidenceTitle = firstBroadcast.Title;

      if(participationCheck is not null)
      {
         activity.LinkedEntityIds = (
            await ResolveLinkedEntityIdsAsync(
               firstBroadcast.EntityId,
               selectableEntities,
               participationCheck.Participants,
               cancellationToken
            )
         ).ToList();
      }

      return firstBroadcast.EntityId;
   }

   public async Task<Guid> QueueTeaserAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      return await aiJobRunner.QueueAsync(
         new AiJobRequest(
            AiJobIds.GenerateActivityTeaser,
            await CreateActivityAiInputJsonAsync(
               activity,
               cancellationToken
            ),
            activity.Id?.ToString()
         ),
         cancellationToken
      );
   }

   public async Task<Guid> QueueFactsAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      return await aiJobRunner.QueueAsync(
         new AiJobRequest(
            AiJobIds.FindActivityFacts,
            await CreateActivityAiInputJsonAsync(
               activity,
               cancellationToken
            ),
            activity.Id?.ToString()
         ),
         cancellationToken
      );
   }

   private async Task<
      IReadOnlyList<BroadcastEntityOption>
   > GetEntityPickerOptionsAsync(
      IReadOnlySet<Guid> selectedEntityIds,
      Guid? organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      if(organizationEntityId is not null)
      {
         return await GetSelectableEntitiesAsync(
            organizationEntityId,
            cancellationToken
         );
      }

      if(selectedEntityIds.Count == 0)
      {
         return [];
      }

      var entities = await repository.GetEntityOptionsAsync(
         cancellationToken
      );

      return entities
         .Where(entity => selectedEntityIds.Contains(entity.Id))
         .Select(ToBroadcastEntityOption)
         .ToList();
   }

   private async Task<
      IReadOnlyList<BroadcastEntityOption>
   > GetSelectableEntitiesAsync(
      Guid? organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      var entities = organizationEntityId is null
         ? await repository.GetEntityOptionsAsync(cancellationToken)
         : await repository.GetPersonEntitiesForOrganizationAsync(
            organizationEntityId.Value,
            cancellationToken
         );

      return entities.Select(ToBroadcastEntityOption).ToList();
   }

   private async Task<IReadOnlyList<Guid>> ResolveLinkedEntityIdsAsync(
      Guid? organizationEntityId,
      IReadOnlyList<BroadcastEntityOption> selectableEntities,
      IReadOnlyCollection<string> participantNames,
      CancellationToken cancellationToken
   )
   {
      if(organizationEntityId is null || participantNames.Count == 0)
      {
         return [];
      }

      var selectablePersonIds = selectableEntities
         .Where(entity => entity.Type == TrackedEntityTypeIds.Person)
         .Select(entity => entity.Id)
         .ToHashSet();
      var entityIdsByName = (
         await adminRepository.GetParticipantEntityNameOptionsAsync(
            organizationEntityId.Value,
            cancellationToken
         )
      )
         .Where(entity => !string.IsNullOrWhiteSpace(entity.Name))
         .GroupBy(entity => BroadcastEntityFilter.NormalizeName(entity.Name))
         .Where(group => !string.IsNullOrWhiteSpace(group.Key))
         .ToDictionary(group => group.Key, group => group.First().Id);
      var linkedEntityIds = new List<Guid>();
      var seenEntityIds = new HashSet<Guid>();

      foreach(var participantName in participantNames)
      {
         var normalizedName = BroadcastEntityFilter.NormalizeParticipantName(
            participantName
         );

         if(string.IsNullOrWhiteSpace(normalizedName))
         {
            continue;
         }

         if(!entityIdsByName.TryGetValue(normalizedName, out var entityId))
         {
            continue;
         }

         var linkedEntity = await adminRepository.GetEntityForEditAsync(
            entityId,
            cancellationToken
         );

         if(linkedEntity is null)
         {
            continue;
         }

         if(string.Equals(
            linkedEntity.EntityTypeId,
            TrackedEntityTypeIds.Pair,
            StringComparison.OrdinalIgnoreCase))
         {
            foreach(var linkedEntityId in linkedEntity.LinkedEntityIds)
            {
               if(!selectablePersonIds.Contains(linkedEntityId) ||
                  !seenEntityIds.Add(linkedEntityId))
               {
                  continue;
               }

               linkedEntityIds.Add(linkedEntityId);
            }

            continue;
         }

         if(linkedEntity.Id is null ||
            !selectablePersonIds.Contains(linkedEntity.Id.Value) ||
            !seenEntityIds.Add(linkedEntity.Id.Value))
         {
            continue;
         }

         linkedEntityIds.Add(linkedEntity.Id.Value);
      }

      return linkedEntityIds;
   }

   private async Task<string> CreateActivityAiInputJsonAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      var selectedIds = (activity.LinkedEntityIds ?? []).ToHashSet();
      var entityNames = await GetSelectableEntitiesAsync(
         null,
         cancellationToken
      );

      var selectedParticipantNames = entityNames
         .Where(entity => selectedIds.Contains(entity.Id))
         .Select(entity => entity.Name)
         .ToList();

      var sportName = (await repository.GetSportOptionsAsync(
         cancellationToken
      ))
         .FirstOrDefault(sport => sport.Id == activity.SportId)
         ?.Label ?? activity.SportId;

      return JsonSerializer.Serialize(
         new
         {
            event_name = activity.Title,
            title = activity.Title,
            description = activity.Description,
            activity_type = activity.ActivityType,
            sport = sportName,
            activity_date = DateDisplay.Format(activity.ActivityDate),
            local_start_time = activity.LocalStartTime?.ToString("HH:mm"),
            time_zone_id = activity.TimeZoneId,
            participants = CreatePromptListText(selectedParticipantNames),
            related_entities = Array.Empty<string>()
         }
      );
   }

   private static string CreatePromptListText(IReadOnlyList<string> values)
   {
      return values.Count == 0
         ? string.Empty
         : string.Join(
            Environment.NewLine,
            values.Select(value => $"  - {value}")
         );
   }

   private static List<Guid> NormalizeBroadcastIds(
      IEnumerable<Guid> ids
   )
   {
      return BroadcastActivityPrefillBuilder.NormalizeBroadcastIds(ids)
         .ToList();
   }

   private static string FormatEntityLabel(BroadcastEntityOption entity)
   {
      var name = string.IsNullOrWhiteSpace(entity.AliasName)
         ? entity.Name
         : $"{entity.Name} [aka {entity.AliasName}]";

      if(entity.Type == TrackedEntityTypeIds.Person &&
         !string.IsNullOrWhiteSpace(entity.Organization))
      {
         return $"{name} ({FormatEntityTypeLabel(entity.Type)}/" +
            $"{entity.Sport}/{entity.Organization})";
      }

      return $"{name} ({FormatEntityTypeLabel(entity.Type)}/" +
         $"{entity.Sport})";
   }

   private static string FormatEntityTypeLabel(string entityTypeId)
   {
      return entityTypeId switch
      {
         TrackedEntityTypeIds.Person => "Person",
         TrackedEntityTypeIds.NationalTeam => "National team",
         TrackedEntityTypeIds.Organization => "Organization",
         _ => entityTypeId
      };
   }

   private static BroadcastEntityOption ToBroadcastEntityOption(
      EntityOption entity
   )
   {
      return new BroadcastEntityOption(
         entity.Id,
         entity.Name,
         entity.Type,
         entity.Sport,
         entity.Organization,
         entity.AliasName
      );
   }
}

public sealed record ActivityEditOptions(
   IReadOnlyList<SelectListItem> Entities,
   IReadOnlyList<SelectListItem> OrganizationEntities,
   IReadOnlyList<LookupOption> ActivityTypes,
   IReadOnlyList<LookupOption> Sports,
   string? LoadError
);
