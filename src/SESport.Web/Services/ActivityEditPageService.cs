using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.AI.Interfaces;
using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Web.Services;

public sealed class ActivityEditPageService(
   ActivityRepository repository,
   AdminRepository adminRepository,
   AdminBroadcastRepository broadcastRepository,
   BroadcastParticipationService participationService,
   IAiJobRunner aiJobRunner,
   ActivityAiInputBuilder aiInputBuilder,
   IAiAutomationService automationService,
   IHostApplicationLifetime applicationLifetime,
   ILogger<ActivityEditPageService> logger
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

         var organizations = (
            await adminRepository.GetOrganizationEntityOptionsAsync(
               cancellationToken,
               sportId
            )
         ).ToList();

         if(organizationEntityId is not null &&
            organizations.All(
               entity => entity.Id != organizationEntityId.Value
            ))
         {
            organizations.AddRange(
               await adminRepository.GetEntityLinkOptionsByIdsAsync(
                  [organizationEntityId.Value],
                  null,
                  cancellationToken
               )
            );
         }

         var organizationOptions = organizations
            .OrderBy(entity => entity.Name)
            .Select(entity => new SelectListItem(
               $"{entity.Name} ({entity.Sport})",
               entity.Id.ToString(),
               entity.Id == organizationEntityId
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
         when(!cancellationToken.IsCancellationRequested)
      {
         logger.LogError(
            exception,
            "Unable to load activity edit options."
         );

         return new ActivityEditOptions(
            [],
            [],
            [],
            [],
            PageModelErrorExtensions.UnexpectedErrorMessage
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

   public async Task<IReadOnlyList<string>> LoadOtherGroupDescriptionsAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      if(!string.IsNullOrWhiteSpace(activity.Description) ||
         activity.ActivityGroupId is null)
      {
         return [];
      }

      return await repository.GetOtherGroupDescriptionsAsync(
         activity.ActivityGroupId.Value,
         activity.Id,
         cancellationToken
      );
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

   public async Task SetParticipantActiveAsync(
      Guid activityId,
      Guid entityId,
      bool isActive,
      CancellationToken cancellationToken
   )
   {
      await repository.SetParticipantActiveAsync(
         activityId,
         entityId,
         isActive,
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
      var isNew = activity.Id is null;

      if(activity.ActivityGroupCreationRequired &&
         activity.ActivityGroupId is null)
      {
         activity.ActivityGroupTitle = await ResolveActivityGroupTitleAsync(
            activity.BroadcastIds,
            cancellationToken
         ) ?? activity.ActivityGroupTitle;
      }

      var activityId = await repository.SaveAsync(
         activity,
         cancellationToken
      );
      if(activity.ParticipationRunId is not null)
      {
         await participationService.RecordApplicationAsync(
            activity.ParticipationRunId.Value,
            activityId,
            activity.BroadcastIds,
            cancellationToken
         );
      }
      await broadcastRepository.HideAsync(
         NormalizeBroadcastIds(activity.BroadcastIds),
         cancellationToken
      );

      if(isNew)
      {
         await automationService.HandleActivityCreatedAsync(
            activityId,
            applicationLifetime.ApplicationStopping
         );
      }
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
      var localStart = TimeZoneHelper.ToLocal(
         firstBroadcast.StartsAt,
         SportDay.TimeZoneId
      );
      IReadOnlyList<ActivityGroupParticipant> groupParticipants = [];

      activity.BroadcastIds = [firstBroadcast.Id];
      activity.OrganizationEntityId = firstBroadcast.EntityId;
      activity.TvChannelName = firstBroadcast.ChannelName;

      if(firstBroadcast.ActivityGroupSourceActivityId is not null)
      {
         activity.ActivityGroupId = await repository.GetActivityGroupIdAsync(
            firstBroadcast.ActivityGroupSourceActivityId.Value,
            cancellationToken
         );

         if(activity.ActivityGroupId is not null)
         {
            var participantsByGroup = await repository
               .GetActivityGroupParticipantsAsync(
                  [activity.ActivityGroupId.Value],
                  cancellationToken
               );
            if(participantsByGroup.TryGetValue(
                  activity.ActivityGroupId.Value,
                  out var knownParticipants
               ))
            {
               groupParticipants = knownParticipants;
            }
         }
      }

      var participationCheck =
         participationRunId is null && groupParticipants.Count > 0
            ? null
            : await participationService.GetParticipationCheckAsync(
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

      activity.ActivityGroupCreationRequired =
         string.Equals(
            firstBroadcast.ActivityGroupSourceKindId,
            BroadcastActivitySourceKindIds.ActivityGroupForActivity,
            StringComparison.Ordinal
         ) &&
         activity.ActivityGroupId is null;

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
      var evidenceExcerpt = BroadcastActivityPrefillBuilder
         .CreateEvidenceComment(firstBroadcast, participationCheck);
      activity.Sources = participationCheck?.SourceUrls
         .Select(
            url => new ActivitySourceEditModel
            {
               Kind = SourceKinds.ParticipationEvidence,
               Url = url,
               Title = firstBroadcast.Title,
               Excerpt = evidenceExcerpt
            }
         )
         .ToList() ?? [];

      if(!string.IsNullOrWhiteSpace(firstBroadcast.EntitySportId))
      {
         activity.SportId = firstBroadcast.EntitySportId;
      }

      activity.ActivityDate = DateOnly.FromDateTime(localStart.DateTime);
      activity.LocalStartTime = TimeOnly.FromDateTime(localStart.DateTime);
      var localEnd = TimeZoneHelper.ToLocal(
         firstBroadcast.EndsAt,
         SportDay.TimeZoneId
      );
      activity.LocalEndTime = TimeOnly.FromDateTime(localEnd.DateTime);
      activity.TimeZoneId = SportDay.TimeZoneId;

      if(participationCheck is not null)
      {
         activity.ParticipationRunId = participationCheck.RunId;
         activity.LinkedEntityIds = (
            await ResolveLinkedEntityIdsAsync(
               firstBroadcast.EntityId,
               selectableEntities,
               participationCheck.Participants,
               cancellationToken
            )
         ).ToList();
      }
      else if(groupParticipants.Count > 0)
      {
         activity.LinkedEntityIds = groupParticipants
            .Select(participant => participant.Id)
            .ToList();
      }

      return firstBroadcast.EntityId;
   }

   private async Task<string?> ResolveActivityGroupTitleAsync(
      IReadOnlyCollection<Guid> broadcastIds,
      CancellationToken cancellationToken
   )
   {
      var normalizedIds = NormalizeBroadcastIds(broadcastIds);

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

      var draftBroadcast = broadcasts.FirstOrDefault(
         broadcast =>
            !string.IsNullOrWhiteSpace(broadcast.ActivityGroupDraftTitle)
      );

      if(draftBroadcast is not null)
      {
         return draftBroadcast.ActivityGroupDraftTitle;
      }

      return broadcasts[0].Title;
   }

   public async Task<Guid> QueueTeaserAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      return await aiJobRunner.QueueAsync(
         new AiJobRequest(
            AiJobIds.GenerateActivityTeaser,
            await aiInputBuilder.BuildAsync(
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
            await aiInputBuilder.BuildAsync(
               activity,
               cancellationToken,
               activity.ActivityGroupTitle
            ),
            activity.Id?.ToString()
         ),
         cancellationToken
      );
   }

   public async Task<Guid> QueueFindParticipantsStartAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      return await QueueFindParticipantsAsync(
         activity,
         AiJobIds.FindParticipantsStart,
         cancellationToken
      );
   }

   public async Task<Guid> QueueFindParticipantsResultAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      return await QueueFindParticipantsAsync(
         activity,
         AiJobIds.FindParticipantsResult,
         cancellationToken
      );
   }

   private async Task<Guid> QueueFindParticipantsAsync(
      ActivityEditModel activity,
      string jobId,
      CancellationToken cancellationToken
   )
   {
      return await aiJobRunner.QueueAsync(
         new AiJobRequest(
            jobId,
            await aiInputBuilder.BuildAsync(
               activity,
               cancellationToken,
               activity.ActivityGroupTitle
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
      var linkedEntityIds = new List<Guid>();
      var seenEntityIds = new HashSet<Guid>();

      foreach(var participantName in participantNames)
      {
         if(!BroadcastEntityFilter.TryGetNameMatch(
               entityIdsByName,
               participantName,
               out var entityId
            ) &&
            !BroadcastEntityFilter.TryGetFuzzyNameMatch(
               entityIdsByName,
               participantName,
               out entityId
            ))
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
