using Npgsql;
using System.Text.Json;

using SESport.AI.Interfaces;
using SESport.Core.Configuration;
using SESport.Core.Formatting;
using SESport.Data;
using SESport.Data.AI;
using SESport.Web.Services;

namespace SESport.Core.Tests.Services;

public sealed class ActivityEditPageServiceTests
{
   [Fact]
   public async Task LoadOptionsAsyncShowsOnlyOrganizationPersons()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var pairId = Guid.NewGuid();

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

      try
      {
         var options = await fixture.Service.LoadOptionsAsync(
            [],
            organizationId,
            CancellationToken.None
         );

         Assert.Contains(
            options.Entities,
            option => option.Value == personId.ToString()
         );
         Assert.DoesNotContain(
            options.Entities,
            option => option.Value == pairId.ToString()
         );
      }
      finally
      {
         await DeleteLinksAsync(dataSource, personId);
         await DeleteLinksAsync(dataSource, pairId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, pairId);
         await DeleteEntityAsync(dataSource, organizationId);
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
   public async Task QueueFactsAsyncIncludesOrganizationType()
   {
      var organizationId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var fixture = CreateFixture(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         "Diamond League",
         TrackedEntityTypeIds.Organization,
         "football"
      );

      try
      {
         var activity = new ActivityEditModel
         {
            Title = "Friidrott - London",
            Description = "Description",
            ActivityType = ActivityType.Match.ToString(),
            SportId = "football",
            ActivityDate = new DateOnly(2026, 7, 15),
            OrganizationEntityId = organizationId
         };

         await fixture.Service.QueueFactsAsync(
            activity,
            CancellationToken.None
         );

         var request = Assert.Single(fixture.JobRunner.Requests);
         using var document = JsonDocument.Parse(request.InputPayloadJson);

         Assert.Equal(
            "Diamond League",
            document.RootElement.GetProperty("type").GetString()
         );
         Assert.Equal(
            "Friidrott - London",
            document.RootElement.GetProperty("title").GetString()
         );
      }
      finally
      {
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
         new DateOnly(2026, 7, 15),
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      var endsAt = TimeZoneHelper.ToUtc(
         new DateOnly(2026, 7, 15),
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
         Assert.Equal(new DateOnly(2026, 7, 15), activity.ActivityDate);
         Assert.Equal(new TimeOnly(12, 0), activity.LocalStartTime);
         Assert.Equal(new TimeOnly(14, 0), activity.LocalEndTime);
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
      var startDate = new DateOnly(2026, 7, 15);

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
      }
      finally
      {
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
            new DateOnly(2026, 7, 15),
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            new DateOnly(2026, 7, 15),
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
            new DateOnly(2026, 7, 15),
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            new DateOnly(2026, 7, 15),
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
            new DateOnly(2026, 7, 15),
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            new DateOnly(2026, 7, 15),
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
            ActivityDate = new DateOnly(2026, 7, 15),
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
            ActivityDate = new DateOnly(2026, 7, 10),
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
      }
      finally
      {
         await DeleteEntityAsync(dataSource, firstPersonId);
         await DeleteEntityAsync(dataSource, secondPersonId);
      }
   }

   private static NpgsqlDataSource CreateDataSource()
   {
      return new NpgsqlDataSourceBuilder(
         PostgresConnectionStrings.ResolveDefault()
      ).Build();
   }

   private static ActivityEditPageServiceFixture CreateFixture(
      NpgsqlDataSource dataSource
   )
   {
      var activityRepository = new ActivityRepository(dataSource);
      var broadcastRepository = new AdminBroadcastRepository(dataSource);
      var jobRunner = new CapturingAiJobRunner();
      var participationService = new BroadcastParticipationService(
         activityRepository,
         new AiRepository(dataSource),
         new AdminRepository(dataSource),
         broadcastRepository,
         jobRunner
      );

      return new ActivityEditPageServiceFixture(
         new ActivityEditPageService(
            activityRepository,
            new AdminRepository(dataSource),
            broadcastRepository,
            participationService,
            jobRunner
         ),
         participationService,
         jobRunner
      );
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
      CapturingAiJobRunner JobRunner
   );

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
            'se',
            'NationalityOrSportingIdentity',
            'Test coverage',
            'tier_3',
            'short_term'
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
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
                  "Name": "{{participantName}}",
                  "Sources": [
                     {
                        "Url": "https://example.test/participant",
                        "EvidenceType": "ParticipantMention"
                     }
                  ]
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
