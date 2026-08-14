using System.Text.Json;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using Npgsql;

using SESport.AI.Jobs;
using SESport.Core.Configuration;
using SESport.Core.Formatting;
using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Core.Tests.Services;

public sealed class ActivityEditPageServiceTests
{
   [Fact]
   public async Task LoadOptionsAsyncShowsOnlyOrganizationPersons()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var pairId = Guid.NewGuid();
      var activityGroupId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {personId:N}",
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         pairId,
         $"Pair {pairId:N}",
         TrackedEntityTypeIds.Pair,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, personId, organizationId);
      await InsertEntityLinkAsync(dataSource, pairId, organizationId);
      await InsertActivityGroupAsync(
         dataSource,
         activityGroupId,
         $"Activity Group {activityGroupId:N}",
         "football",
         DistantActivityDate,
         DistantActivityDate
      );
      try
      {
         var options = await fixture.Service.LoadOptionsAsync(
            [],
            organizationId,
            CancellationToken.None,
            "football"
         );

         Assert.Contains(
            options.Entities,
            option => option.Value == personId.ToString()
         );
         Assert.DoesNotContain(
            options.Entities,
            option => option.Value == pairId.ToString()
         );
         var groups = await fixture.Service.SearchActivityGroupsAsync(
            "Activity Group",
            "football",
            CancellationToken.None
         );
         Assert.Contains(
            groups,
            option => option.Id == activityGroupId.ToString()
         );
      }
      finally
      {
         await DeleteLinksAsync(dataSource, personId);
         await DeleteLinksAsync(dataSource, pairId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, pairId);
         await DeleteEntityAsync(dataSource, organizationId);
         await DeleteActivityGroupAsync(dataSource, activityGroupId);
      }
   }

   [Fact]
   public async Task LoadOptionsAsyncWithoutOrganizationDoesNotShowAllPersons()
   {
      var personId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {personId:N}",
         TrackedEntityTypeIds.Person,
         "football"
      );

      try
      {
         var options = await fixture.Service.LoadOptionsAsync(
            [],
            null,
            CancellationToken.None
         );

         Assert.DoesNotContain(
            options.Entities,
            option => option.Value == personId.ToString()
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, personId);
      }
   }

   [Fact]
   public async Task LoadOptionsAsyncKeepsSelectedCrossSportOrganization()
   {
      var organizationId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      try
      {
         await InsertRelatedEntityAsync(
            dataSource,
            organizationId,
            $"Tour {organizationId:N}",
            TrackedEntityTypeIds.Tour,
            "cycling"
         );

         var options = await fixture.Service.LoadOptionsAsync(
            [],
            organizationId,
            CancellationToken.None,
            "mountain-biking"
         );

         var organization = Assert.Single(
            options.OrganizationEntities,
            option => option.Value == organizationId.ToString()
         );
         Assert.True(organization.Selected);
      }
      finally
      {
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task QueueFactsAsyncTargetsActivityGroup()
   {
      var organizationId = Guid.NewGuid();
      var activityGroupId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         "Diamond League",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertActivityGroupAsync(
         dataSource,
         activityGroupId,
         "Diamond League London",
         "football",
         DistantActivityDate,
         DistantActivityDate
      );

      try
      {
         var activity = new ActivityEditModel
         {
            Title = "Friidrott - London",
            ActivityGroupId = activityGroupId,
            ActivityGroupTitle = "Diamond League London",
            Description = "Description",
            ActivityType = ActivityType.Match.ToString(),
            SportId = "football",
            ActivityDate = DistantActivityDate,
            OrganizationEntityId = organizationId
         };

         await fixture.Service.QueueFactsAsync(
            activity,
            CancellationToken.None
         );

         var request = Assert.Single(fixture.JobRunner.Requests);
         using var document = JsonDocument.Parse(request.InputPayloadJson);

         Assert.Equal(
            AiJobIds.FindActivityGroupFacts,
            request.JobId
         );
         Assert.Equal(
            "Diamond League London",
            document.RootElement.GetProperty("title").GetString()
         );
      }
      finally
      {
         await DeleteActivityGroupAsync(dataSource, activityGroupId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task PrefillFromBroadcastsAsyncUsesDirectOrgPersons()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var personName = $"Person {Guid.NewGuid():N}";
      var startsAt = TimeZoneHelper.ToUtc(
         DistantActivityDate,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      var endsAt = TimeZoneHelper.ToUtc(
         DistantActivityDate,
         new TimeOnly(14, 0),
         SportDay.TimeZoneId
      );

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);
      var jobContext = await LoadParticipationJobContextAsync(
         dataSource,
         "decide-swedish-participation"
      );
      var runId = Guid.NewGuid();

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         personName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, personId, organizationId);
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         organizationId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         "Broadcast title",
         ["football"],
         startsAt,
         endsAt
      );
      await InsertRunAsync(
         dataSource,
         runId,
         "decide-swedish-participation",
         jobContext.PromptId,
         jobContext.ProviderId,
         broadcastId.ToString(),
         personName
      );

      Guid? savedActivityId = null;

      try
      {
         var activity = new ActivityEditModel();

         await fixture.Service.PrefillFromBroadcastsAsync(
            activity,
            [broadcastId],
            null,
            CancellationToken.None
         );

         Assert.Equal([broadcastId], activity.BroadcastIds);
         Assert.Equal(runId, activity.ParticipationRunId);
         Assert.Equal(organizationId, activity.OrganizationEntityId);
         Assert.Equal([personId], activity.LinkedEntityIds);
         Assert.Equal("football", activity.SportId);
         Assert.Equal(DistantActivityDate, activity.ActivityDate);
         Assert.Equal(new TimeOnly(12, 0), activity.LocalStartTime);
         Assert.Equal(new TimeOnly(14, 0), activity.LocalEndTime);

         await fixture.Service.SaveAsync(activity, CancellationToken.None);
         var activityInfo = await GetActivityInfoAsync(
            dataSource,
            activity.Title,
            activity.ActivityDate!.Value,
            activity.SportId
         );
         savedActivityId = activityInfo.ActivityId;

         await using var applicationCommand = dataSource.CreateCommand(
            """
            select count(*)
            from ai_job_run_applications
            where run_id = @run_id
               and target_type = @target_type
               and target_id = @target_id
            """
         );
         applicationCommand.Parameters.AddWithValue("run_id", runId);
         applicationCommand.Parameters.AddWithValue(
            "target_type",
            AiJobRunApplicationTargetTypes.Activity
         );
         applicationCommand.Parameters.AddWithValue(
            "target_id",
            savedActivityId.Value.ToString()
         );

         Assert.Equal(
            1L,
            (long)(await applicationCommand.ExecuteScalarAsync())!
         );

         await using var broadcastLinkCommand = dataSource.CreateCommand(
            """
            select count(*)
            from activity_broadcast_links
            where activity_id = @activity_id
               and broadcast_id = @broadcast_id
            """
         );
         broadcastLinkCommand.Parameters.AddWithValue(
            "activity_id",
            savedActivityId.Value
         );
         broadcastLinkCommand.Parameters.AddWithValue(
            "broadcast_id",
            broadcastId
         );

         Assert.Equal(
            1L,
            (long)(await broadcastLinkCommand.ExecuteScalarAsync())!
         );
      }
      finally
      {
         if(savedActivityId is not null)
         {
            await DeleteActivityAsync(dataSource, savedActivityId.Value);
         }

         await DeleteParticipationRunAsync(dataSource, runId);
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteLinksAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task PrefillFromBroadcastsAsyncLinksFuzzyOrgPersons()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var personName = "Johan Grönvall";
      var aiParticipantName = "Johan Görnvall";

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);
      var jobContext = await LoadParticipationJobContextAsync(
         dataSource,
         "decide-swedish-participation"
      );
      var runId = Guid.NewGuid();

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         personName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, personId, organizationId);
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         organizationId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         "Broadcast title",
         ["football"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );
      await InsertRunAsync(
         dataSource,
         runId,
         "decide-swedish-participation",
         jobContext.PromptId,
         jobContext.ProviderId,
         broadcastId.ToString(),
         aiParticipantName
      );

      try
      {
         var activity = new ActivityEditModel();

         await fixture.Service.PrefillFromBroadcastsAsync(
            activity,
            [broadcastId],
            null,
            CancellationToken.None
         );

         Assert.Equal([broadcastId], activity.BroadcastIds);
         Assert.Equal(organizationId, activity.OrganizationEntityId);
         Assert.Equal([personId], activity.LinkedEntityIds);
      }
      finally
      {
         await DeleteParticipationRunAsync(dataSource, runId);
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteLinksAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task PrefillFromBroadcastsAsyncReusesExistingActivityGroup()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var broadcastId = Guid.NewGuid();
      var activityGroupId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var title = "Broadcast title";
      var startDate = DistantActivityDate;

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);
      var broadcastRepository = new AdminBroadcastRepository(dataSource);
      var jobContext = await LoadParticipationJobContextAsync(
         dataSource,
         "decide-swedish-participation"
      );
      var runId = Guid.NewGuid();

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {personId:N}",
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, personId, organizationId);
      await InsertActivityGroupAsync(
         dataSource,
         activityGroupId,
         title,
         "football",
         startDate,
         startDate
      );
      var activityId = await InsertActivityAsync(
         dataSource,
         activityGroupId,
         organizationId,
         personId,
         title,
         "football",
         startDate
      );
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         organizationId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         title,
         ["football"],
         TimeZoneHelper.ToUtc(
            startDate,
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            startDate,
            new TimeOnly(14, 0),
            SportDay.TimeZoneId
         )
      );
      await broadcastRepository.UpdateOrganizationAsync(
         broadcastId,
         organizationId,
         CancellationToken.None
      );
      var broadcastSource = Assert.Single(
         await broadcastRepository.GetActivitySourcesAsync(
            [broadcastId],
            CancellationToken.None
         )
      );

      Assert.Equal(
         BroadcastActivitySourceKindIds.ActivityGroupForActivity,
         broadcastSource.ActivityGroupSourceKindId
      );
      Assert.Equal(activityId, broadcastSource.ActivityGroupSourceActivityId);
      await InsertRunAsync(
         dataSource,
         runId,
         "decide-swedish-participation",
         jobContext.PromptId,
         jobContext.ProviderId,
         broadcastId.ToString(),
         "Unknown participant"
      );

      try
      {
         var activity = new ActivityEditModel();

         await fixture.Service.PrefillFromBroadcastsAsync(
            activity,
            [broadcastId],
            null,
            CancellationToken.None
         );

         Assert.Equal(activityGroupId, activity.ActivityGroupId);
         Assert.False(activity.ActivityGroupCreationRequired);
         Assert.Equal([personId], activity.LinkedEntityIds);

         var broadcast = Assert.IsType<BroadcastListItem>(
            await broadcastRepository.GetByIdAsync(
               broadcastId,
               CancellationToken.None
            )
         );
         var displayedBroadcast = Assert.Single(
            await fixture.ParticipationService
               .ApplyParticipationChecksAsync(
                  [broadcast],
                  CancellationToken.None
               )
         );
         Assert.Equal(
            [personId],
            displayedBroadcast.ActivityGroupParticipants
               .Select(participant => participant.Id)
         );

         var clearedActivity = new ActivityEditModel();
         await fixture.Service.PrefillFromBroadcastsAsync(
            clearedActivity,
            [broadcastId],
            null,
            CancellationToken.None,
            true
         );

         Assert.Equal(activityGroupId, clearedActivity.ActivityGroupId);
         Assert.Empty(clearedActivity.LinkedEntityIds);
         Assert.Null(clearedActivity.ParticipationRunId);
      }
      finally
      {
         await DeleteParticipationRunAsync(dataSource, runId);
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteActivityGroupAsync(dataSource, activityGroupId);
         await DeleteLinksAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task PrefillFromBroadcastsAsyncRequiresNewActivityGroup()
   {
      var organizationId = Guid.NewGuid();
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);
      var broadcastRepository = new AdminBroadcastRepository(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         organizationId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         "Broadcast title",
         ["football"],
         TimeZoneHelper.ToUtc(
            DistantActivityDate,
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            DistantActivityDate,
            new TimeOnly(14, 0),
            SportDay.TimeZoneId
         )
      );
      await broadcastRepository.UpdateOrganizationAsync(
         broadcastId,
         organizationId,
         CancellationToken.None
      );
      var broadcastSource = Assert.Single(
         await broadcastRepository.GetActivitySourcesAsync(
            [broadcastId],
            CancellationToken.None
         )
      );

      Assert.Equal(
         BroadcastActivitySourceKindIds.ActivityGroupForActivity,
         broadcastSource.ActivityGroupSourceKindId
      );
      Assert.Null(broadcastSource.ActivityGroupSourceActivityId);

      try
      {
         var activity = new ActivityEditModel();

         await fixture.Service.PrefillFromBroadcastsAsync(
            activity,
            [broadcastId],
            null,
            CancellationToken.None
         );

         Assert.Null(activity.ActivityGroupId);
         Assert.True(activity.ActivityGroupCreationRequired);
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task SaveAsyncUsesBroadcastDraftActivityGroupTitle()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var broadcastTitle = $"Broadcast {uniqueSuffix}";
      var groupTitle = $"Draft group {uniqueSuffix}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {personId:N}",
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, personId, organizationId);
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         organizationId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         broadcastTitle,
         ["football"],
         TimeZoneHelper.ToUtc(
            DistantActivityDate,
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            DistantActivityDate,
            new TimeOnly(14, 0),
            SportDay.TimeZoneId
         ),
         groupTitle,
         BroadcastActivitySourceKindIds.ActivityGroupForActivity
      );

      Guid? savedActivityId = null;
      Guid? savedActivityGroupId = null;

      try
      {
         var activity = new ActivityEditModel();

         await fixture.Service.PrefillFromBroadcastsAsync(
            activity,
            [broadcastId],
            null,
            CancellationToken.None
         );

         Assert.True(activity.ActivityGroupCreationRequired);

         await fixture.Service.SaveAsync(activity, CancellationToken.None);

         var activityInfo = await GetActivityInfoAsync(
            dataSource,
            activity.Title,
            activity.ActivityDate!.Value,
            activity.SportId
         );

         savedActivityId = activityInfo.ActivityId == Guid.Empty
            ? null
            : activityInfo.ActivityId;
         savedActivityGroupId = activityInfo.ActivityGroupId;

         Assert.NotEqual(Guid.Empty, activityInfo.ActivityId);
         Assert.NotNull(activityInfo.ActivityGroupId);
         Assert.Equal(groupTitle, activity.ActivityGroupTitle);
         Assert.False(activity.ActivityGroupCreationRequired);
         Assert.Equal(
            [activityInfo.ActivityGroupId!.Value],
            fixture.AutomationService.ActivityGroupCreatedIds
         );
         AssertActivityGroupTitle(
            dataSource,
            activityInfo.ActivityGroupId!.Value,
            groupTitle
         );
      }
      finally
      {
         if(savedActivityId is not null)
         {
            await DeleteActivityAsync(dataSource, savedActivityId.Value);
         }

         if(savedActivityGroupId is not null)
         {
            await DeleteActivityGroupAsync(
               dataSource,
               savedActivityGroupId.Value
            );
         }

         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteLinksAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task SaveAsyncSkipsActivityGroupWhenDraftIsCleared()
   {
      var organizationId = Guid.NewGuid();
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var broadcastTitle = $"Broadcast {uniqueSuffix}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);
      var broadcastRepository = new AdminBroadcastRepository(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         organizationId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         broadcastTitle,
         ["football"],
         TimeZoneHelper.ToUtc(
            DistantActivityDate,
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            DistantActivityDate,
            new TimeOnly(14, 0),
            SportDay.TimeZoneId
         )
      );
      await broadcastRepository.UpdateOrganizationAsync(
         broadcastId,
         organizationId,
         CancellationToken.None
      );
      await broadcastRepository.UpdateActivityGroupTitleAsync(
         broadcastId,
         string.Empty,
         CancellationToken.None
      );

      Guid? savedActivityId = null;

      try
      {
         var activity = new ActivityEditModel();

         await fixture.Service.PrefillFromBroadcastsAsync(
            activity,
            [broadcastId],
            null,
            CancellationToken.None
         );

         Assert.Null(activity.ActivityGroupId);
         Assert.False(activity.ActivityGroupCreationRequired);

         await fixture.Service.SaveAsync(activity, CancellationToken.None);

         var activityInfo = await GetActivityInfoAsync(
            dataSource,
            activity.Title,
            activity.ActivityDate!.Value,
            activity.SportId
         );

         savedActivityId = activityInfo.ActivityId == Guid.Empty
            ? null
            : activityInfo.ActivityId;

         Assert.NotEqual(Guid.Empty, activityInfo.ActivityId);
         Assert.Null(activityInfo.ActivityGroupId);
      }
      finally
      {
         if(savedActivityId is not null)
         {
            await DeleteActivityAsync(dataSource, savedActivityId.Value);
         }

         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task SaveAsyncCreatesActivityGroupWhenRequired()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {personId:N}",
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, personId, organizationId);

      Guid? savedActivityId = null;
      Guid? savedActivityGroupId = null;

      try
      {
         var model = new ActivityEditModel
         {
            Title = "Broadcast title",
            ActivityType = ActivityType.Match.ToString(),
            SportId = "football",
            ActivityDate = DistantActivityDate,
            LocalStartTime = new TimeOnly(12, 0),
            LocalEndTime = new TimeOnly(14, 0),
            TimeZoneId = SportDay.TimeZoneId,
            LinkedEntityIds = [personId],
            OrganizationEntityId = organizationId,
            ActivityGroupCreationRequired = true
         };

         savedActivityId = await repository.SaveAsync(
            model,
            CancellationToken.None
         );
         var savedActivity = await repository.GetForEditAsync(
            savedActivityId.Value,
            CancellationToken.None
         );

         Assert.NotNull(savedActivity);
         Assert.NotNull(savedActivity!.ActivityGroupId);
         Assert.Equal("Broadcast title", savedActivity.ActivityGroupTitle);
         Assert.Equal(new TimeOnly(14, 0), savedActivity.LocalEndTime);
         Assert.Equal(savedActivity.ActivityGroupId, model.ActivityGroupId);
         Assert.False(model.ActivityGroupCreationRequired);
         savedActivityGroupId = savedActivity.ActivityGroupId;
      }
      finally
      {
         if(savedActivityId is not null)
         {
            await repository.DeleteAsync(
               savedActivityId.Value,
               CancellationToken.None
            );
         }

         if(savedActivityGroupId is not null)
         {
            await DeleteActivityGroupAsync(
               dataSource,
               savedActivityGroupId.Value
            );
         }

         await DeleteLinksAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task SaveAsyncUsesApplicationLifetimeForAutomation()
   {
      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);
      var requestCancellation = new CancellationTokenSource();
      var title = $"Automation token {Guid.NewGuid():N}";
      Guid? savedActivityId = null;

      try
      {
         var activity = new ActivityEditModel
         {
            Title = title,
            ActivityType = ActivityType.Match.ToString(),
            SportId = "football",
            ActivityDate = DistantActivityDate,
            TimeZoneId = SportDay.TimeZoneId
         };

         await fixture.Service.SaveAsync(
            activity,
            requestCancellation.Token
         );

         var activityInfo = await GetActivityInfoAsync(
            dataSource,
            title,
            DistantActivityDate,
            "football"
         );
         savedActivityId = activityInfo.ActivityId;

         Assert.Equal(
            fixture.ApplicationLifetime.ApplicationStopping,
            fixture.AutomationService.CancellationToken
         );
         Assert.NotEqual(
            requestCancellation.Token,
            fixture.AutomationService.CancellationToken
         );
      }
      finally
      {
         requestCancellation.Dispose();

         if(savedActivityId is not null)
         {
            await DeleteActivityAsync(dataSource, savedActivityId.Value);
         }
      }
   }

   [Fact]
   public async Task LoadOtherGroupDescriptionsAsyncReturnsUniqueDescriptions()
   {
      var activityGroupId = Guid.NewGuid();
      var title = $"Description group {Guid.NewGuid():N}";
      var activityIds = new List<Guid>();

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);
      var repository = new ActivityRepository(dataSource);

      await InsertActivityGroupAsync(
         dataSource,
         activityGroupId,
         title,
         "football",
         DistantActivityDate,
         DistantActivityDate.AddDays(2)
      );

      try
      {
         foreach(var description in new[]
         {
            "First group description",
            "Second group description",
            "First group description"
         })
         {
            activityIds.Add(
               await repository.SaveAsync(
                  new ActivityEditModel
                  {
                     Title = title,
                     Description = description,
                     ActivityType = ActivityType.Match.ToString(),
                     SportId = "football",
                     ActivityDate = DistantActivityDate,
                     TimeZoneId = SportDay.TimeZoneId,
                     ActivityGroupId = activityGroupId
                  },
                  CancellationToken.None
               )
            );
         }

         var descriptions = await fixture.Service
            .LoadOtherGroupDescriptionsAsync(
               new ActivityEditModel
               {
                  ActivityGroupId = activityGroupId
               },
               CancellationToken.None
            );

         Assert.Equal(2, descriptions.Count);
         Assert.Contains("First group description", descriptions);
         Assert.Contains("Second group description", descriptions);
      }
      finally
      {
         foreach(var activityId in activityIds)
         {
            await DeleteActivityAsync(dataSource, activityId);
         }

         await DeleteActivityGroupAsync(dataSource, activityGroupId);
      }
   }

   [Fact]
   public async Task PrefillFromBroadcastsAsyncExpandsPairParticipants()
   {
      var organizationId = Guid.NewGuid();
      var pairId = Guid.NewGuid();
      var firstPersonId = Guid.NewGuid();
      var secondPersonId = Guid.NewGuid();
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var pairName = $"Pair {Guid.NewGuid():N}";
      var firstPersonName = $"First {Guid.NewGuid():N}";
      var secondPersonName = $"Second {Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);
      var jobContext = await LoadParticipationJobContextAsync(
         dataSource,
         "decide-swedish-participation"
      );
      var runId = Guid.NewGuid();

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         pairId,
         pairName,
         TrackedEntityTypeIds.Pair,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         firstPersonId,
         firstPersonName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         secondPersonId,
         secondPersonName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, pairId, organizationId);
      await InsertEntityLinkAsync(dataSource, pairId, firstPersonId);
      await InsertEntityLinkAsync(dataSource, pairId, secondPersonId);
      await InsertEntityLinkAsync(dataSource, firstPersonId, organizationId);
      await InsertEntityLinkAsync(dataSource, secondPersonId, organizationId);
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         organizationId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         "Broadcast title",
         ["football"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );
      await InsertRunAsync(
         dataSource,
         runId,
         "decide-swedish-participation",
         jobContext.PromptId,
         jobContext.ProviderId,
         broadcastId.ToString(),
         pairName
      );

      try
      {
         var activity = new ActivityEditModel();

         await fixture.Service.PrefillFromBroadcastsAsync(
            activity,
            [broadcastId],
            null,
            CancellationToken.None
         );

         Assert.Equal(2, activity.LinkedEntityIds.Count);
         Assert.Contains(firstPersonId, activity.LinkedEntityIds);
         Assert.Contains(secondPersonId, activity.LinkedEntityIds);
      }
      finally
      {
         await DeleteParticipationRunAsync(dataSource, runId);
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteLinksAsync(dataSource, pairId);
         await DeleteLinksAsync(dataSource, firstPersonId);
         await DeleteLinksAsync(dataSource, secondPersonId);
         await DeleteEntityAsync(dataSource, firstPersonId);
         await DeleteEntityAsync(dataSource, secondPersonId);
         await DeleteEntityAsync(dataSource, pairId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task QueueTeaserAsyncFormatsParticipantsLikeCandidates()
   {
      var firstPersonId = Guid.NewGuid();
      var secondPersonId = Guid.NewGuid();
      var firstPersonName = $"First {Guid.NewGuid():N}";
      var secondPersonName = $"Second {Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         firstPersonId,
         firstPersonName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         secondPersonId,
         secondPersonName,
         TrackedEntityTypeIds.Person,
         "football"
      );

      try
      {
         var activity = new ActivityEditModel
         {
            Title = "Activity title",
            ActivityType = "Match",
            SportId = "football",
            ActivityDate = DistantActivityDate.AddDays(-5),
            LinkedEntityIds = [firstPersonId, secondPersonId]
         };

         await fixture.Service.QueueTeaserAsync(
            activity,
            CancellationToken.None
         );

         var request = Assert.Single(fixture.JobRunner.Requests);
         using var document = System.Text.Json.JsonDocument.Parse(
            request.InputPayloadJson
         );

         Assert.Equal(
            "Activity title",
            document.RootElement.GetProperty("event_name").GetString()
         );
         Assert.Equal(
            $"  - {firstPersonName}{Environment.NewLine}" +
               $"  - {secondPersonName}",
            document.RootElement.GetProperty("participants").GetString()
         );
         var participantEntities = document.RootElement
            .GetProperty("participant_entities");

         Assert.Equal(2, participantEntities.GetArrayLength());
         Assert.Equal(
            firstPersonId.ToString(),
            participantEntities[0].GetProperty("id").GetString()
         );
         Assert.Equal(
            firstPersonName,
            participantEntities[0].GetProperty("name").GetString()
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, firstPersonId);
         await DeleteEntityAsync(dataSource, secondPersonId);
      }
   }

   [Fact]
   public async Task
      QueueFindParticipantsStartAsyncFormatsParticipantsLikeCandidates()
   {
      var firstPersonId = Guid.NewGuid();
      var secondPersonId = Guid.NewGuid();
      var firstPersonName = $"First {Guid.NewGuid():N}";
      var secondPersonName = $"Second {Guid.NewGuid():N}";
      var sportId = $"test-sport-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      try
      {
         await InsertSportAsync(dataSource, sportId, true);
         await InsertRelatedEntityAsync(
            dataSource,
            firstPersonId,
            firstPersonName,
            TrackedEntityTypeIds.Person,
            sportId
         );
         await InsertRelatedEntityAsync(
            dataSource,
            secondPersonId,
            secondPersonName,
            TrackedEntityTypeIds.Person,
            sportId
         );

         var activity = new ActivityEditModel
         {
            Title = "Activity title",
            ActivityGroupTitle = "Activity group title",
            ActivityType = "Match",
            SportId = sportId,
            ActivityDate = DistantActivityDate.AddDays(-5),
            LinkedEntityIds = [firstPersonId, secondPersonId]
         };

         await fixture.Service.QueueFindParticipantsStartAsync(
            activity,
            CancellationToken.None
         );

         var request = Assert.Single(fixture.JobRunner.Requests);
         Assert.Equal(
            AiJobIds.FindParticipantsStart,
            request.JobId
         );

         using var document = System.Text.Json.JsonDocument.Parse(
            request.InputPayloadJson
         );

         Assert.Equal(
            "Activity title",
            document.RootElement.GetProperty("event_name").GetString()
         );
         Assert.Equal(
            "Activity title",
            document.RootElement.GetProperty("title").GetString()
         );
         Assert.Equal(
            $"  - {firstPersonName}{Environment.NewLine}" +
               $"  - {secondPersonName}",
            document.RootElement.GetProperty("participants").GetString()
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, firstPersonId);
         await DeleteEntityAsync(dataSource, secondPersonId);
         await DeleteSportAsync(dataSource, sportId);
      }
   }

   [Fact]
   public async Task
      QueueFindParticipantsStartAsyncIgnoresSportStartTimeRequirement()
   {
      var sportId = $"test-sport-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      try
      {
         await InsertSportAsync(dataSource, sportId, false);

         var activity = new ActivityEditModel
         {
            Title = "Activity title",
            ActivityType = "Match",
            SportId = sportId,
            ActivityDate = DistantActivityDate.AddDays(-5)
         };

         await fixture.Service.QueueFindParticipantsStartAsync(
            activity,
            CancellationToken.None
         );

         var request = Assert.Single(fixture.JobRunner.Requests);
         Assert.Equal(AiJobIds.FindParticipantsStart, request.JobId);
      }
      finally
      {
         await DeleteSportAsync(dataSource, sportId);
      }
   }

   [Fact]
   public async Task
      QueueFindParticipantsResultAsyncFormatsParticipantsLikeCandidates()
   {
      var firstPersonId = Guid.NewGuid();
      var secondPersonId = Guid.NewGuid();
      var firstPersonName = $"First {Guid.NewGuid():N}";
      var secondPersonName = $"Second {Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         firstPersonId,
         firstPersonName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         secondPersonId,
         secondPersonName,
         TrackedEntityTypeIds.Person,
         "football"
      );

      try
      {
         var activity = new ActivityEditModel
         {
            Title = "Activity title",
            ActivityGroupTitle = "Activity group title",
            ActivityType = "Match",
            SportId = "football",
            ActivityDate = DistantActivityDate.AddDays(-5),
            LinkedEntityIds = [firstPersonId, secondPersonId]
         };

         await fixture.Service.QueueFindParticipantsResultAsync(
            activity,
            CancellationToken.None
         );

         var request = Assert.Single(fixture.JobRunner.Requests);
         Assert.Equal(
            AiJobIds.FindParticipantsResult,
            request.JobId
         );

         using var document = System.Text.Json.JsonDocument.Parse(
            request.InputPayloadJson
         );

         Assert.Equal(
            "Activity title",
            document.RootElement.GetProperty("event_name").GetString()
         );
         Assert.Equal(
            "Activity group title",
            document.RootElement.GetProperty("title").GetString()
         );
         Assert.Equal(
            $"  - {firstPersonName}{Environment.NewLine}" +
               $"  - {secondPersonName}",
            document.RootElement.GetProperty("participants").GetString()
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, firstPersonId);
         await DeleteEntityAsync(dataSource, secondPersonId);
      }
   }

   private static ActivityEditPageServiceFixture CreateFixture(
      NpgsqlDataSource dataSource
   )
   {
      var activityRepository = new ActivityRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var broadcastRepository = new AdminBroadcastRepository(dataSource);
      var jobRunner = new CapturingAiJobRunner();
      var participationService = new BroadcastParticipationService(
         activityRepository,
         new AiRepository(dataSource),
         adminRepository,
         broadcastRepository,
         jobRunner
      );
      var inputBuilder = new ActivityAiInputBuilder(
         activityRepository,
         adminRepository
      );
      var automationService = new CapturingAiAutomationService();
      var applicationLifetime = new TestHostApplicationLifetime();

      return new ActivityEditPageServiceFixture(
         new ActivityEditPageService(
            activityRepository,
            adminRepository,
            broadcastRepository,
            participationService,
            jobRunner,
            new AiRepository(dataSource),
            inputBuilder,
            automationService,
            applicationLifetime,
            NullLogger<ActivityEditPageService>.Instance
         ),
         participationService,
         jobRunner,
         automationService,
         applicationLifetime
      );
   }

   private static async Task InsertSportAsync(
      NpgsqlDataSource dataSource,
      string sportId,
      bool requiresParticipantStartTimes
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into sports (
            id,
            name,
            requires_start_time
         )
         values (
            @id,
            @name,
            @requires_start_time
         )
         """;
      command.Parameters.AddWithValue("id", sportId);
      command.Parameters.AddWithValue("name", sportId);
      command.Parameters.AddWithValue(
         "requires_start_time",
         requiresParticipantStartTimes
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteSportAsync(
      NpgsqlDataSource dataSource,
      string sportId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from sports
         where id = @id
         """;
      command.Parameters.AddWithValue("id", sportId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task<(Guid ActivityId, Guid? ActivityGroupId)>
      GetActivityInfoAsync(
         NpgsqlDataSource dataSource,
         string title,
         DateOnly activityDate,
         string sportId
      )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         select id, activity_group_id
         from activities
         where title = @title
            and activity_date = @activity_date
            and sport_id = @sport_id
         order by id
         limit 1
         """;
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue("activity_date", activityDate);
      command.Parameters.AddWithValue("sport_id", sportId);

      await using var reader = await command.ExecuteReaderAsync();

      if(!await reader.ReadAsync())
      {
         return (Guid.Empty, null);
      }

      return (
         reader.GetGuid(0),
         reader.IsDBNull(1) ? null : reader.GetGuid(1)
      );
   }

   private static void AssertActivityGroupTitle(
      NpgsqlDataSource dataSource,
      Guid activityGroupId,
      string expectedTitle
   )
   {
      using var connection = dataSource.OpenConnection();
      using var command = connection.CreateCommand();
      command.CommandText = """
         select title
         from activity_groups
         where id = @id
         """;
      command.Parameters.AddWithValue("id", activityGroupId);

      var actualValue = command.ExecuteScalar();
      Assert.Equal(expectedTitle, actualValue as string);
   }

   private sealed record ActivityEditPageServiceFixture(
      ActivityEditPageService Service,
      BroadcastParticipationService ParticipationService,
      CapturingAiJobRunner JobRunner,
      CapturingAiAutomationService AutomationService,
      TestHostApplicationLifetime ApplicationLifetime
   );

   private sealed class CapturingAiAutomationService : IAiAutomationService
   {
      public CancellationToken CancellationToken { get; private set; }

      public List<Guid> ActivityCreatedIds { get; } = [];

      public List<Guid> ActivityGroupCreatedIds { get; } = [];

      public Task HandleActivityCreatedAsync(
         Guid activityId,
         CancellationToken cancellationToken
      )
      {
         CancellationToken = cancellationToken;
         ActivityCreatedIds.Add(activityId);
         return Task.CompletedTask;
      }

      public Task HandleActivityGroupCreatedAsync(
         Guid activityGroupId,
         CancellationToken cancellationToken
      )
      {
         CancellationToken = cancellationToken;
         ActivityGroupCreatedIds.Add(activityGroupId);
         return Task.CompletedTask;
      }

      public Task HandlePersonCreatedAsync(
         Guid personEntityId,
         CancellationToken cancellationToken
      )
      {
         return Task.CompletedTask;
      }
   }

   private sealed class CapturingAiJobRunner : IAiJobRunner
   {
      public List<AiJobRequest> Requests { get; } = [];

      public Task<Guid> QueueAsync(
         AiJobRequest request,
         CancellationToken cancellationToken
      )
      {
         Requests.Add(request);
         return Task.FromResult(Guid.NewGuid());
      }

      public Task<AiJobResult> RunAsync(
         AiJobRequest request,
         CancellationToken cancellationToken
      )
      {
         Requests.Add(request);

         return Task.FromResult(
            new AiJobResult(
               Guid.NewGuid(),
               request.JobId,
               "test-provider",
               "test-model",
               "prompt",
               "{}",
               """{"teaser":"Kort teaser."}""",
               null,
               null,
               0,
               0,
               null,
               null,
               null,
               null
            )
         );
      }
   }

   private sealed class TestHostApplicationLifetime
      : IHostApplicationLifetime
   {
      public CancellationToken ApplicationStarted => CancellationToken.None;

      public CancellationToken ApplicationStopping => CancellationToken.None;

      public CancellationToken ApplicationStopped => CancellationToken.None;

      public void StopApplication()
      {
      }
   }

   private static async Task InsertBroadcastAsync(
      NpgsqlDataSource dataSource,
      Guid broadcastId,
      string sourceKey,
      Guid? entityId,
      string externalId,
      string fingerprint,
      string channelId,
      string channelName,
      string title,
      string[] categories,
      DateTimeOffset startsAt,
      DateTimeOffset endsAt,
      string? activityGroupDraftTitle = null,
      string? activityGroupSourceKindId = null,
      Guid? activityGroupSourceActivityId = null
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into broadcasts (
            id,
            source_key,
            external_id,
            fingerprint,
            entity_id,
            channel_id,
            channel_name,
            title,
            description,
            categories,
            is_replay,
            original_air_date,
            starts_at,
            ends_at,
            time_zone_id,
            activity_group_source_kind_id,
            activity_group_source_activity_id,
            activity_group_draft_title,
            raw_programme_xml
         )
         values (
            @id,
            @source_key,
            @external_id,
            @fingerprint,
            @entity_id,
            @channel_id,
            @channel_name,
            @title,
            null,
            @categories,
            false,
            null,
            @starts_at,
            @ends_at,
            'Europe/Stockholm',
            @activity_group_source_kind_id,
            @activity_group_source_activity_id,
            @activity_group_draft_title,
            null
         )
         """;
      command.Parameters.AddWithValue("id", broadcastId);
      command.Parameters.AddWithValue("source_key", sourceKey);
      command.Parameters.AddWithValue("external_id", externalId);
      command.Parameters.AddWithValue("fingerprint", fingerprint);
      command.Parameters.AddWithValue(
         "entity_id",
         (object?)entityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue("channel_id", channelId);
      command.Parameters.AddWithValue("channel_name", channelName);
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue("categories", categories);
      command.Parameters.AddWithValue("starts_at", startsAt);
      command.Parameters.AddWithValue("ends_at", endsAt);
      command.Parameters.AddWithValue(
         "activity_group_source_kind_id",
         (object?)activityGroupSourceKindId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "activity_group_source_activity_id",
         (object?)activityGroupSourceActivityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "activity_group_draft_title",
         (object?)activityGroupDraftTitle ?? DBNull.Value
      );

      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId,
      string title,
      string sportId,
      DateOnly startDate,
      DateOnly endDate
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into activity_groups (
            id,
            title,
            sport_id,
            start_date,
            end_date
         )
         values (
            @id,
            @title,
            @sport_id,
            @start_date,
            @end_date
         )
         """;
      command.Parameters.AddWithValue("id", activityGroupId);
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue("sport_id", sportId);
      command.Parameters.AddWithValue("start_date", startDate);
      command.Parameters.AddWithValue("end_date", endDate);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task<Guid> InsertActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId,
      Guid organizationEntityId,
      Guid personId,
      string title,
      string sportId,
      DateOnly activityDate
   )
   {
      var repository = new ActivityRepository(dataSource);

      return await repository.SaveAsync(
         new ActivityEditModel
         {
            Title = title,
            ActivityType = ActivityType.Match.ToString(),
            SportId = sportId,
            ActivityDate = activityDate,
            LocalStartTime = new TimeOnly(12, 0),
            TimeZoneId = SportDay.TimeZoneId,
            LinkedEntityIds = [personId],
            OrganizationEntityId = organizationEntityId,
            ActivityGroupId = activityGroupId
         },
         CancellationToken.None
      );
   }

   private static async Task InsertRelatedEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId,
      string entityName,
      string entityTypeId,
      string sportId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into entities (
            id,
            canonical_name,
            entity_type_id,
            sport_id,
            country_id,
            country_relevance_kind_id,
            country_relevance_reason,
            watch_priority_id,
            expected_stability_id
         )
         values (
            @id,
            @canonical_name,
            @entity_type_id,
            @sport_id,
            @country_id,
            'NationalityOrSportingIdentity',
            'Test coverage',
            'tier_3',
            'short_term'
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
      command.Parameters.AddWithValue("entity_type_id", entityTypeId);
      command.Parameters.AddWithValue("sport_id", sportId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertEntityLinkAsync(
      NpgsqlDataSource dataSource,
      Guid sourceEntityId,
      Guid targetEntityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into entity_to_entity_links (
            id,
            source_entity_id,
            target_entity_id
         )
         values (
            @id,
            @source_entity_id,
            @target_entity_id
         )
         """;
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("source_entity_id", sourceEntityId);
      command.Parameters.AddWithValue("target_entity_id", targetEntityId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertRunAsync(
      NpgsqlDataSource dataSource,
      Guid runId,
      string jobId,
      Guid promptId,
      string providerId,
      string correlationId,
      string participantName
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into ai_job_runs (
            id,
            job_id,
            prompt_id,
            provider_id,
            status_id,
            correlation_id,
            provider_model,
            input_payload,
            rendered_prompt,
            raw_request,
            raw_response,
            tool_trace,
            output_text,
            error_message,
            started_at,
            completed_at,
            duration_seconds,
            input_tokens,
            output_tokens,
            reasoning_tokens,
            tool_round_count,
            conversation_character_count,
            execution_environment
         )
         values (
            @id,
            @job_id,
            @prompt_id,
            @provider_id,
            'completed',
            @correlation_id,
            'gpt',
            '{}'::jsonb,
            'Rendered',
            null,
            null,
            null,
            @output_text::jsonb,
            null,
            now(),
            now(),
            null,
            null,
            null,
            null,
            0,
            0,
            null
         )
         """;
      command.Parameters.AddWithValue("id", runId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("prompt_id", promptId);
      command.Parameters.AddWithValue("provider_id", providerId);
      command.Parameters.AddWithValue("correlation_id", correlationId);
      command.Parameters.AddWithValue(
         "output_text",
         $$"""
         {
            "Participation": "Yes",
            "Participants": [
               {
                  "Name": "{{participantName}}"
               }
            ],
            "CheckedSources": []
         }
         """
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteParticipationRunAsync(
      NpgsqlDataSource dataSource,
      Guid runId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from ai_job_runs
         where id = @id
         """;
      command.Parameters.AddWithValue("id", runId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task<JobContext> LoadParticipationJobContextAsync(
      NpgsqlDataSource dataSource,
      string jobId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         select
            j.provider_id,
            coalesce(j.active_prompt_id, p.id)
         from ai_jobs j
         left join ai_job_prompts p
            on p.job_id = j.id
               and p.enabled = true
         where j.id = @job_id
         order by p.version desc nulls last
         limit 1
         """;
      command.Parameters.AddWithValue("job_id", jobId);

      await using var reader = await command.ExecuteReaderAsync();

      if(!await reader.ReadAsync())
      {
         throw new InvalidOperationException(
            $"Missing AI job definition: {jobId}"
         );
      }

      return new JobContext(
         reader.GetString(0),
         reader.GetGuid(1)
      );
   }

   private static async Task DeleteBroadcastAsync(
      NpgsqlDataSource dataSource,
      Guid broadcastId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from broadcasts
         where id = @id
         """;
      command.Parameters.AddWithValue("id", broadcastId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from activity_groups
         where id = @id
         """;
      command.Parameters.AddWithValue("id", activityGroupId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      var repository = new ActivityRepository(dataSource);
      await repository.DeleteAsync(activityId, CancellationToken.None);
   }

   private static async Task DeleteEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from entities
         where id = @id
         """;
      command.Parameters.AddWithValue("id", entityId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteLinksAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from entity_to_entity_links
         where source_entity_id = @id
            or target_entity_id = @id
         """;
      command.Parameters.AddWithValue("id", entityId);

      await command.ExecuteNonQueryAsync();
   }

   private sealed record JobContext(
      string ProviderId,
      Guid PromptId
   );
}
