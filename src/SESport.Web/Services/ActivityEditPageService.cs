using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.AI.Jobs;
using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Web.Services;

public sealed class ActivityEditPageService(
   ActivityRepository repository,
   AdminRepository adminRepository,
   AdminBroadcastRepository broadcastRepository,
   BroadcastParticipationService participationService,
   IAiJobRunner aiJobRunner,
   IAiJobRunRepository runRepository,
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
      var activity = await repository.GetForEditAsync(
         id,
         cancellationToken
      );

      if(activity?.Id is not Guid activityId)
      {
         return activity;
      }

      activity.OriginatingAiRun = await runRepository
         .GetOriginatingActivityRunAsync(
            activityId,
            cancellationToken
         );

      return activity;
   }

   public async Task<IReadOnlyList<LookupOption>>
      SearchActivityGroupsAsync(
         string? term,
         string? sportId,
         CancellationToken cancellationToken,
         Guid? organizationEntityId = null
      )
   {
      return await repository.SearchActivityGroupOptionsAsync(
         term,
         sportId,
         cancellationToken,
         organizationEntityId
      );
   }

   public async Task<IReadOnlyList<string>> LoadOtherGroupDescriptionsAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      if(activity.ActivityGroupId is null ||
         (activity.Id is not null &&
            !string.IsNullOrWhiteSpace(activity.Description)))
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
      var broadcastSource = await ResolveBroadcastActivitySourceAsync(
         activity.BroadcastIds,
         cancellationToken
      );

      if(isNew &&
         activity.ActivityGroupId is null &&
         string.Equals(
            broadcastSource?.ActivityGroupSourceKindId,
            BroadcastActivitySourceKindIds.ActivityGroupForActivity,
            StringComparison.Ordinal
         ))
      {
         activity.ActivityGroupCreationRequired = true;
      }

      if(activity.ActivityGroupCreationRequired &&
         activity.ActivityGroupId is null)
      {
         activity.ActivityGroupTitle = ResolveActivityGroupTitle(
            broadcastSource
         ) ?? activity.ActivityGroupTitle;

         if(activity.ActivityDate is not null &&
            !string.IsNullOrWhiteSpace(activity.ActivityGroupTitle) &&
            !string.IsNullOrWhiteSpace(activity.SportId))
         {
            activity.ActivityGroupId = await repository
               .FindMatchingActivityGroupIdAsync(
                  activity.ActivityGroupTitle,
                  activity.SportId,
                  activity.ActivityDate.Value,
                  cancellationToken
               );

            if(activity.ActivityGroupId is not null)
            {
               activity.ActivityGroupTitle = await repository
                  .GetActivityGroupTitleAsync(
                     activity.ActivityGroupId.Value,
                     cancellationToken
                  );
               activity.ActivityGroupCreationRequired = false;
            }
         }
      }

      activity.AutoMergeActivityId = null;
      activity.AutoMergeActivityTitle = null;
      if(isNew && activity.BroadcastIds.Count > 0)
      {
         var autoMergeCandidate = await repository
            .FindAutoMergeActivityAsync(activity, cancellationToken);

         if(autoMergeCandidate is not null)
         {
            var merged = await repository.MergeBroadcastIntoActivityAsync(
               autoMergeCandidate.Id,
               activity,
               cancellationToken
            );
            if(merged)
            {
               await RecordParticipationApplicationAsync(
                  activity,
                  autoMergeCandidate.Id,
                  cancellationToken
               );
               await broadcastRepository.HideAsync(
                  NormalizeBroadcastIds(activity.BroadcastIds),
                  cancellationToken
               );
               return;
            }
         }
      }

      var createsActivityGroup = activity.ActivityGroupId is null &&
         activity.ActivityGroupCreationRequired;

      var activityId = await repository.SaveAsync(
         activity,
         cancellationToken
      );
      if(activity.ParticipationRunId is not null)
      {
         await RecordParticipationApplicationAsync(
            activity,
            activityId,
            cancellationToken
         );
      }
      await broadcastRepository.HideAsync(
         NormalizeBroadcastIds(activity.BroadcastIds),
         cancellationToken
      );

      if(createsActivityGroup && activity.ActivityGroupId is not null)
      {
         await TryHandleAutomationAsync(
            AiAutomationEventIds.ActivityGroupCreated,
            activityId,
            cancellationToken =>
               automationService.HandleActivityGroupCreatedAsync(
                  activity.ActivityGroupId.Value,
                  cancellationToken
               )
         );
      }

      if(isNew)
      {
         await TryHandleAutomationAsync(
            AiAutomationEventIds.ActivityCreated,
            activityId,
            cancellationToken =>
               automationService.HandleActivityCreatedAsync(
                  activityId,
                  cancellationToken
               )
         );
      }
   }

   private async Task TryHandleAutomationAsync(
      string eventId,
      Guid activityId,
      Func<CancellationToken, Task> handler
   )
   {
      try
      {
         await handler(applicationLifetime.ApplicationStopping);
      }
      catch(Exception exception)
      {
         logger.LogError(
            exception,
            "Activity {ActivityId} was saved, but AI automation " +
               "event {EventId} failed.",
            activityId,
            eventId
         );
      }
   }

   public async Task<Guid?> PrefillFromBroadcastsAsync(
      ActivityEditModel activity,
      IReadOnlyCollection<Guid> ids,
      Guid? participationRunId,
      CancellationToken cancellationToken,
      bool clearParticipants = false
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
      activity.ActivityDate = DateOnly.FromDateTime(localStart.DateTime);

      if(!string.IsNullOrWhiteSpace(firstBroadcast.EntitySportId))
      {
         activity.SportId = firstBroadcast.EntitySportId;
      }

      if(clearParticipants)
      {
         activity.LinkedEntityIds = [];
         activity.ParticipationRunId = null;
      }

      if(firstBroadcast.ActivityGroupSourceActivityId is not null)
      {
         activity.ActivityGroupId = await repository.GetActivityGroupIdAsync(
            firstBroadcast.ActivityGroupSourceActivityId.Value,
            cancellationToken
         );

         if(activity.ActivityGroupId is not null)
         {
            activity.ActivityGroupTitle = await repository
               .GetActivityGroupTitleAsync(
                  activity.ActivityGroupId.Value,
                  cancellationToken
               );
            groupParticipants = await LoadActivityGroupParticipantsAsync(
               activity.ActivityGroupId.Value,
               clearParticipants,
               cancellationToken
            );
         }
      }

      if(activity.ActivityGroupId is null &&
         string.Equals(
            firstBroadcast.ActivityGroupSourceKindId,
            BroadcastActivitySourceKindIds.ActivityGroupForActivity,
            StringComparison.Ordinal
         ) &&
         activity.ActivityDate is not null &&
         !string.IsNullOrWhiteSpace(activity.SportId))
      {
         var groupTitle = firstBroadcast.ActivityGroupDraftTitle;
         if(string.IsNullOrWhiteSpace(groupTitle))
         {
            groupTitle = firstBroadcast.Title;
         }

         activity.ActivityGroupTitle = groupTitle;

         activity.ActivityGroupId = await repository
            .FindMatchingActivityGroupIdAsync(
               groupTitle,
               activity.SportId,
               activity.ActivityDate.Value,
               cancellationToken
            );

         if(activity.ActivityGroupId is not null)
         {
            activity.ActivityGroupTitle = await repository
               .GetActivityGroupTitleAsync(
                  activity.ActivityGroupId.Value,
                  cancellationToken
               );
            groupParticipants = await LoadActivityGroupParticipantsAsync(
               activity.ActivityGroupId.Value,
               clearParticipants,
               cancellationToken
            );
         }
      }

      var skipParticipationCheck = clearParticipants ||
         participationRunId is null && groupParticipants.Count > 0;
      var participationCheck = skipParticipationCheck
         ? null
         : await participationService.GetParticipationCheckAsync(
            firstBroadcast.Id,
            participationRunId,
            cancellationToken
         );

      if(participationCheck is not null &&
         participationCheck.IsPositive &&
         firstBroadcast.EntityId is not null)
      {
         await participationService
            .EnsureMatchedParticipantOrganizationLinksAsync(
               firstBroadcast.EntityId.Value,
               participationCheck.Participants,
               cancellationToken
            );
      }

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
            firstBroadcast.Categories,
            activity.SportId
         )?.ToString() ?? string.Empty;
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

      var autoMergeCandidate = await repository
         .FindAutoMergeActivityAsync(activity, cancellationToken);
      activity.AutoMergeActivityId = autoMergeCandidate?.Id;
      activity.AutoMergeActivityTitle = autoMergeCandidate?.Title;
      if(autoMergeCandidate is not null)
      {
         activity.Description = autoMergeCandidate.Description;
      }

      return firstBroadcast.EntityId;
   }

   private async Task RecordParticipationApplicationAsync(
      ActivityEditModel activity,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      if(activity.ParticipationRunId is null)
      {
         return;
      }

      await participationService.RecordApplicationAsync(
         activity.ParticipationRunId.Value,
         activityId,
         activity.BroadcastIds,
         cancellationToken
      );
   }

   private async Task<IReadOnlyList<ActivityGroupParticipant>>
      LoadActivityGroupParticipantsAsync(
         Guid activityGroupId,
         bool clearParticipants,
         CancellationToken cancellationToken
      )
   {
      if(clearParticipants)
      {
         return [];
      }

      var participantsByGroup = await repository
         .GetActivityGroupParticipantsAsync(
            [activityGroupId],
            cancellationToken
         );

      return participantsByGroup.TryGetValue(
         activityGroupId,
         out var knownParticipants
      )
         ? knownParticipants
         : [];
   }

   private async Task<BroadcastActivitySource?>
      ResolveBroadcastActivitySourceAsync(
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

      return broadcasts[0];
   }

   private static string? ResolveActivityGroupTitle(
      BroadcastActivitySource? broadcast
   )
   {
      if(broadcast is null)
      {
         return null;
      }

      return !string.IsNullOrWhiteSpace(broadcast.ActivityGroupDraftTitle)
         ? broadcast.ActivityGroupDraftTitle
         : broadcast.Title;
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
      if(activity.ActivityGroupId is null)
      {
         throw new InvalidOperationException(
            "The activity must belong to an ActivityGroup before finding " +
            "group facts."
         );
      }

      var correlationId = activity.ActivityGroupId.Value.ToString();
      var existingRunId = await runRepository.GetActiveRunIdAsync(
         AiJobIds.FindActivityGroupFacts,
         correlationId,
         cancellationToken
      );

      if(existingRunId is not null)
      {
         return existingRunId.Value;
      }

      return await aiJobRunner.QueueAsync(
         new AiJobRequest(
            AiJobIds.FindActivityGroupFacts,
            await aiInputBuilder.BuildActivityGroupAsync(
               activity.ActivityGroupId.Value,
               cancellationToken
            ),
            correlationId
         ),
         cancellationToken
      );
   }

   public async Task<Guid> QueueActivityAiJobAsync(
      string jobId,
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      return jobId switch
      {
         AiJobIds.FindActivityGroupFacts => await QueueFactsAsync(
            activity,
            cancellationToken
         ),
         AiJobIds.FindParticipantsStart =>
            await QueueFindParticipantsStartAsync(
               activity,
               cancellationToken
            ),
         AiJobIds.FindParticipantsResult =>
            await QueueFindParticipantsResultAsync(
               activity,
               cancellationToken
            ),
         _ => throw new ArgumentException(
            "The AI job is not supported for activities.",
            nameof(jobId)
         )
      };
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
      var promptTitle = jobId == AiJobIds.FindParticipantsStart
         ? null
         : activity.ActivityGroupTitle;
      return await aiJobRunner.QueueAsync(
         new AiJobRequest(
            jobId,
            await aiInputBuilder.BuildAsync(
               activity,
               cancellationToken,
               promptTitle
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
      var entityOptionsById = entityOptions
         .GroupBy(option => option.Id)
         .ToDictionary(group => group.Key, group => group.First());
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
               if(IsExcludedFromPrimaryCountryParticipation(
                     entityOptionsById,
                     linkedEntityId
                  ) ||
                  !selectablePersonIds.Contains(linkedEntityId) ||
                  !seenEntityIds.Add(linkedEntityId))
               {
                  continue;
               }

               linkedEntityIds.Add(linkedEntityId);
            }

            continue;
         }

         if(linkedEntity.Id is null ||
            IsExcludedFromPrimaryCountryParticipation(
               entityOptionsById,
               linkedEntity.Id.Value
            ) ||
            !selectablePersonIds.Contains(linkedEntity.Id.Value) ||
            !seenEntityIds.Add(linkedEntity.Id.Value))
         {
            continue;
         }

         linkedEntityIds.Add(linkedEntity.Id.Value);
      }

      return linkedEntityIds;
   }

   private static bool IsExcludedFromPrimaryCountryParticipation(
      IReadOnlyDictionary<Guid, EntityNameOption> entityOptionsById,
      Guid entityId
   )
   {
      return entityOptionsById.TryGetValue(entityId, out var entityOption) &&
         string.Equals(
            entityOption.PrimaryCountryParticipationStatusId,
            PrimaryCountryParticipationStatusIds.RepresentsOtherCountry,
            StringComparison.Ordinal
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
