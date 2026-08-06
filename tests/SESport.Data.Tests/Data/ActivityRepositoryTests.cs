using System.Reflection;

using Npgsql;

using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Core.Tests.Data;

public sealed class ActivityRepositoryTests
{
   [Fact]
   public void GetSportIconPathNormalizesBoatRacing()
   {
      var path = InvokeGetSportIconPath("boat-racing");

      Assert.Equal("/icons/sports/boat-racing.svg", path);
   }

   [Fact]
   public void GetSportIconPathReturnsPathForKnownAsset()
   {
      var path = InvokeGetSportIconPath("motorsport");

      Assert.Equal("/icons/sports/motorsport.svg", path);
   }

   [Fact]
   public async Task SaveAsyncAcceptsPersonWithoutOrganization()
   {
      var personId = Guid.NewGuid();
      Guid? activityId = null;

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      try
      {
         await InsertEntityAsync(
            dataSource,
            personId,
            "Person Without Organization",
            TrackedEntityTypeIds.Person
         );

         activityId = await repository.SaveAsync(
            new ActivityEditModel
            {
               Title = "Activity Without Organization",
               ActivityType = "Match",
               SportId = "football",
               ActivityDate = DistantActivityDate,
               TimeZoneId = SportDay.TimeZoneId,
               LinkedEntityIds = [personId]
            },
            CancellationToken.None
         );

         await using var command = dataSource.CreateCommand(
            """
            select organization_entity_id
            from activity_entity_links
            where activity_id = @activity_id
               and entity_id = @entity_id
            """
         );
         command.Parameters.AddWithValue("activity_id", activityId.Value);
         command.Parameters.AddWithValue("entity_id", personId);

         Assert.Equal(DBNull.Value, await command.ExecuteScalarAsync());
      }
      finally
      {
         if(activityId is not null)
         {
            await DeleteActivityEntityLinksAsync(
               dataSource,
               activityId.Value
            );
            await DeleteActivityAsync(dataSource, activityId.Value);
         }

         await DeleteEntityAsync(dataSource, personId);
      }
   }

   [Fact]
   public async Task GetActivitiesAsyncIncludesSeriesOrganizations()
   {
      var selectedDate = DistantActivityDate;
      var startsAt = TimeZoneHelper.ToUtc(
         selectedDate,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      var activityId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var seriesId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertActivityAsync(
         dataSource,
         activityId,
         selectedDate,
         startsAt
      );
      await InsertEntityAsync(
         dataSource,
         personId,
         "Test Person",
         TrackedEntityTypeIds.Person
      );
      await InsertEntityAsync(
         dataSource,
         seriesId,
         "Test Series",
         TrackedEntityTypeIds.Series
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         activityId,
         personId,
         seriesId
      );
      await InsertEntityLinkAsync(dataSource, personId, seriesId);

      try
      {
         var activities = await repository.GetActivitiesAsync(
            selectedDate,
            ActivityListStatusIds.All,
            [],
            CancellationToken.None
         );

         var activity = Assert.Single(
            activities,
            item => item.Id == activityId
         );
         Assert.Equal("Test Series", activity.RelatedOrganizationEntities);
      }
      finally
      {
         await DeleteLinksAsync(dataSource, personId);
         await DeleteActivityEntityLinksAsync(dataSource, activityId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteEntityAsync(dataSource, seriesId);
         await DeleteEntityAsync(dataSource, personId);
      }
   }

   [Fact]
   public async Task GetActivitiesAsyncPrefersOrganizationAliasName()
   {
      var selectedDate = DistantActivityDate;
      var startsAt = TimeZoneHelper.ToUtc(
         selectedDate,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      var activityId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var seriesId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertActivityAsync(
         dataSource,
         activityId,
         selectedDate,
         startsAt
      );
      await InsertEntityAsync(
         dataSource,
         personId,
         "Test Person",
         TrackedEntityTypeIds.Person
      );
      await InsertEntityAsync(
         dataSource,
         seriesId,
         "Test Series",
         TrackedEntityTypeIds.Series,
         aliasName: "Series Alias"
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         activityId,
         personId,
         seriesId
      );
      await InsertEntityLinkAsync(dataSource, personId, seriesId);

      try
      {
         var activities = await repository.GetActivitiesAsync(
            selectedDate,
            ActivityListStatusIds.All,
            [],
            CancellationToken.None
         );

         var activity = Assert.Single(
            activities,
            item => item.Id == activityId
         );
         Assert.Equal("Series Alias", activity.RelatedOrganizationEntities);
         Assert.Equal(
            "Test Series",
            activity.RelatedOrganizationCanonicalEntities
         );
      }
      finally
      {
         await DeleteLinksAsync(dataSource, personId);
         await DeleteActivityEntityLinksAsync(dataSource, activityId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteEntityAsync(dataSource, seriesId);
         await DeleteEntityAsync(dataSource, personId);
      }
   }

   [Fact]
   public async Task GetActivitiesAsyncPrefersActivityOrganizationContext()
   {
      var selectedDate = DistantActivityDate;
      var startsAt = TimeZoneHelper.ToUtc(
         selectedDate,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      var activityId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var firstSeriesId = Guid.NewGuid();
      var secondSeriesId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertActivityAsync(
         dataSource,
         activityId,
         selectedDate,
         startsAt
      );
      await InsertEntityAsync(
         dataSource,
         personId,
         "Test Person",
         TrackedEntityTypeIds.Person
      );
      await InsertEntityAsync(
         dataSource,
         firstSeriesId,
         "First Series",
         TrackedEntityTypeIds.Series
      );
      await InsertEntityAsync(
         dataSource,
         secondSeriesId,
         "Second Series",
         TrackedEntityTypeIds.Series
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         activityId,
         personId,
         firstSeriesId
      );
      await InsertEntityLinkAsync(dataSource, personId, firstSeriesId);
      await InsertEntityLinkAsync(dataSource, personId, secondSeriesId);

      try
      {
         var activities = await repository.GetActivitiesAsync(
            selectedDate,
            ActivityListStatusIds.All,
            [],
            CancellationToken.None
         );

         var activity = Assert.Single(
            activities,
            item => item.Id == activityId
         );
         Assert.Equal("First Series", activity.RelatedOrganizationEntities);
      }
      finally
      {
         await DeleteLinksAsync(dataSource, personId);
         await DeleteActivityEntityLinksAsync(dataSource, activityId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteEntityAsync(dataSource, secondSeriesId);
         await DeleteEntityAsync(dataSource, firstSeriesId);
         await DeleteEntityAsync(dataSource, personId);
      }
   }

   [Fact]
   public async Task GetActivitiesAsyncDoesNotInferOrganizationContext()
   {
      var selectedDate = DistantActivityDate;
      var startsAt = TimeZoneHelper.ToUtc(
         selectedDate,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      var activityId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var seriesId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertActivityAsync(
         dataSource,
         activityId,
         selectedDate,
         startsAt
      );
      await InsertEntityAsync(
         dataSource,
         personId,
         "Test Person",
         TrackedEntityTypeIds.Person
      );
      await InsertEntityAsync(
         dataSource,
         seriesId,
         "Test Series",
         TrackedEntityTypeIds.Series
      );
      await InsertActivityEntityLinkAsync(dataSource, activityId, personId);
      await InsertEntityLinkAsync(dataSource, personId, seriesId);

      try
      {
         var activities = await repository.GetActivitiesAsync(
            selectedDate,
            ActivityListStatusIds.All,
            [],
            CancellationToken.None
         );

         var activity = Assert.Single(
            activities,
            item => item.Id == activityId
         );
         Assert.Equal(string.Empty, activity.RelatedOrganizationEntities);
      }
      finally
      {
         await DeleteLinksAsync(dataSource, personId);
         await DeleteActivityEntityLinksAsync(dataSource, activityId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteEntityAsync(dataSource, seriesId);
         await DeleteEntityAsync(dataSource, personId);
      }
   }

   [Fact]
   public async Task GetPublishedForDateAsyncOrdersInactiveParticipantsLast()
   {
      var selectedDate = DistantActivityDate;
      var startsAt = TimeZoneHelper.ToUtc(
         selectedDate,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      var activityId = Guid.NewGuid();
      var tierOneId = Guid.NewGuid();
      var reviewAlphaId = Guid.NewGuid();
      var reviewBravoId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertActivityAsync(
         dataSource,
         activityId,
         selectedDate,
         startsAt,
         ActivityPublicationStatusIds.Published
      );
      await InsertEntityAsync(
         dataSource,
         tierOneId,
         "Zulu Tier",
         TrackedEntityTypeIds.Person,
         "tier_1"
      );
      await InsertEntityAsync(
         dataSource,
         reviewAlphaId,
         "Alpha Review",
         TrackedEntityTypeIds.Person,
         "tier_3"
      );
      await InsertEntityAsync(
         dataSource,
         reviewBravoId,
         "Bravo Review",
         TrackedEntityTypeIds.Person,
         "tier_3"
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         activityId,
         reviewBravoId
      );
      await InsertActivityEntityLinkAsync(dataSource, activityId, tierOneId);
      await InsertActivityEntityLinkAsync(
         dataSource,
         activityId,
         reviewAlphaId
      );
      await repository.SetParticipantActiveAsync(
         activityId,
         tierOneId,
         false,
         CancellationToken.None
      );

      try
      {
         var activities = await repository.GetPublishedForDateAsync(
            selectedDate,
            CancellationToken.None
         );

         var activity = Assert.Single(
            activities,
            item => item.Id == activityId
         );
         Assert.Equal(
            "Zulu Tier, Alpha Review, Bravo Review",
            activity.RelatedPersonEntities
         );
         Assert.Equal(
            ["Alpha Review", "Bravo Review", "Zulu Tier"],
            activity.Participants.Select(participant => participant.Name)
         );
         Assert.Equal(
            [reviewAlphaId, reviewBravoId],
            activity.ActiveRelatedPersonEntityIds
         );
      }
      finally
      {
         await DeleteActivityEntityLinksAsync(dataSource, activityId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteEntityAsync(dataSource, reviewBravoId);
         await DeleteEntityAsync(dataSource, reviewAlphaId);
         await DeleteEntityAsync(dataSource, tierOneId);
      }
   }

   [Fact]
   public async Task GetPublishedForDateAsyncIncludesParticipantStartTime()
   {
      var selectedDate = DistantActivityDate;
      var startsAt = TimeZoneHelper.ToUtc(
         selectedDate,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      var activityId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var runId = Guid.NewGuid();
      var sourceUrl = "https://example.test/start-time";
      var source = new SourceEvidenceDraft(sourceUrl, null, null);

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);
      var resultRepository = new ActivityParticipantAiResultRepository(
         dataSource
      );
      var jobContext = await LoadJobContextAsync(
         dataSource,
         AiJobIds.FindParticipantsStart
      );

      await InsertActivityAsync(
         dataSource,
         activityId,
         selectedDate,
         startsAt,
         ActivityPublicationStatusIds.Published
      );
      await InsertEntityAsync(
         dataSource,
         personId,
         "Start Time Person",
         TrackedEntityTypeIds.Person
      );
      await InsertActivityEntityLinkAsync(dataSource, activityId, personId);
      await InsertRunAsync(
         dataSource,
         runId,
         AiJobIds.FindParticipantsStart,
         jobContext.PromptId,
         jobContext.ProviderId,
         activityId
      );
      await resultRepository.UpsertAsync(
         new ActivityParticipantAiResultDraft(
            activityId,
            AiJobIds.FindParticipantsStart,
            runId,
            [source],
            [
               new ActivityParticipantAiResultValueDraft(
                  personId,
                  "start_time",
                  "12:30",
                  "\"12:30\"",
                  [source]
               )
            ]
         ),
         CancellationToken.None
      );

      try
      {
         var activities = await repository.GetPublishedForDateAsync(
            selectedDate,
            CancellationToken.None
         );

         var activity = Assert.Single(
            activities,
            item => item.Id == activityId
         );
         var participant = Assert.Single(activity.Participants);
         Assert.Equal("12:30", participant.StartTime);
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeleteActivityEntityLinksAsync(dataSource, activityId);
         await DeleteParticipantAiSourcesAsync(dataSource, activityId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteEntityAsync(dataSource, personId);
      }
   }

   [Fact]
   public async Task GetPublishedForDateAsyncMarksNationalTeamActivities()
   {
      var selectedDate = DistantActivityDate;
      var startsAt = TimeZoneHelper.ToUtc(
         selectedDate,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      var nationalTeamActivityId = Guid.NewGuid();
      var regularActivityId = Guid.NewGuid();
      var nationalTeamPersonId = Guid.NewGuid();
      var regularPersonId = Guid.NewGuid();
      var nationalTeamOrganizationId = Guid.NewGuid();
      var seriesOrganizationId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertActivityAsync(
         dataSource,
         nationalTeamActivityId,
         selectedDate,
         startsAt,
         ActivityPublicationStatusIds.Published
      );
      await InsertActivityAsync(
         dataSource,
         regularActivityId,
         selectedDate,
         startsAt,
         ActivityPublicationStatusIds.Published
      );
      await InsertEntityAsync(
         dataSource,
         nationalTeamPersonId,
         "National Team Person",
         TrackedEntityTypeIds.Person
      );
      await InsertEntityAsync(
         dataSource,
         regularPersonId,
         "Regular Person",
         TrackedEntityTypeIds.Person
      );
      await InsertEntityAsync(
         dataSource,
         nationalTeamOrganizationId,
         "National Team Organization",
         TrackedEntityTypeIds.NationalTeam
      );
      await InsertEntityAsync(
         dataSource,
         seriesOrganizationId,
         "Series Organization",
         TrackedEntityTypeIds.Series
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         nationalTeamActivityId,
         nationalTeamPersonId,
         nationalTeamOrganizationId
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         regularActivityId,
         regularPersonId,
         seriesOrganizationId
      );

      try
      {
         var activities = await repository.GetPublishedForDateAsync(
            selectedDate,
            CancellationToken.None
         );

         var nationalTeamActivity = Assert.Single(
            activities,
            item => item.Id == nationalTeamActivityId
         );
         var regularActivity = Assert.Single(
            activities,
            item => item.Id == regularActivityId
         );

         Assert.True(
            nationalTeamActivity.HasNationalTeamRelatedOrganization
         );
         Assert.False(
            regularActivity.HasNationalTeamRelatedOrganization
         );
      }
      finally
      {
         await DeleteActivityEntityLinksAsync(
            dataSource,
            nationalTeamActivityId
         );
         await DeleteActivityEntityLinksAsync(dataSource, regularActivityId);
         await DeleteActivityAsync(dataSource, nationalTeamActivityId);
         await DeleteActivityAsync(dataSource, regularActivityId);
         await DeleteEntityAsync(dataSource, nationalTeamOrganizationId);
         await DeleteEntityAsync(dataSource, seriesOrganizationId);
         await DeleteEntityAsync(dataSource, nationalTeamPersonId);
         await DeleteEntityAsync(dataSource, regularPersonId);
      }
   }

   [Fact]
   public async Task GetParticipantsForEditAsyncOrdersDistinctRows()
   {
      var activityId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var alphaId = Guid.NewGuid();
      var betaId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization
      );
      await InsertEntityAsync(
         dataSource,
         alphaId,
         "Alpha Person",
         TrackedEntityTypeIds.Person
      );
      await InsertEntityAsync(
         dataSource,
         betaId,
         "Beta Person",
         TrackedEntityTypeIds.Person
      );
      await InsertActivityAsync(
         dataSource,
         activityId,
         DistantActivityDate,
         TimeZoneHelper.ToUtc(
            DistantActivityDate,
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         )
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         activityId,
         alphaId,
         organizationId
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         activityId,
         betaId,
         organizationId
      );

      try
      {
         var participants = await repository.GetParticipantsForEditAsync(
            null,
            [alphaId, betaId],
            CancellationToken.None
         );

         Assert.Collection(
            participants,
            participant => Assert.Equal("Alpha Person", participant.Name),
            participant => Assert.Equal("Beta Person", participant.Name)
         );
      }
      finally
      {
         await DeleteActivityEntityLinksAsync(dataSource, activityId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteLinksAsync(dataSource, alphaId);
         await DeleteLinksAsync(dataSource, betaId);
         await DeleteEntityAsync(dataSource, betaId);
         await DeleteEntityAsync(dataSource, alphaId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task SetParticipantActiveAsyncUpdatesEditStatus()
   {
      var activityId = Guid.NewGuid();
      var personId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertEntityAsync(
         dataSource,
         personId,
         "Inactive Person",
         TrackedEntityTypeIds.Person
      );
      await InsertActivityAsync(
         dataSource,
         activityId,
         DistantActivityDate,
         TimeZoneHelper.ToUtc(
            DistantActivityDate,
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         )
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         activityId,
         personId
      );

      try
      {
         await repository.SetParticipantActiveAsync(
            activityId,
            personId,
            false,
            CancellationToken.None
         );
         var participants = await repository.GetParticipantsForEditAsync(
            activityId,
            [],
            CancellationToken.None
         );

         var participant = Assert.Single(participants);
         Assert.False(participant.IsActive);
      }
      finally
      {
         await DeleteActivityEntityLinksAsync(dataSource, activityId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteEntityAsync(dataSource, personId);
      }
   }

   [Fact]
   public async Task SetParticipantActiveAsyncUpdatesFutureGroupEntries()
   {
      var groupId = Guid.NewGuid();
      var selectedActivityId = Guid.NewGuid();
      var futureActivityId = Guid.NewGuid();
      var pastActivityId = Guid.NewGuid();
      var unrelatedActivityId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var pastDate = new DateOnly(1900, 1, 1);
      var futureDate = DistantActivityDate.AddDays(1);

      await using var dataSource = CreateDataSource();
      var repository = new ActivityRepository(dataSource);

      await InsertEntityAsync(
         dataSource,
         personId,
         "Group Participant",
         TrackedEntityTypeIds.Person
      );
      await InsertActivityGroupAsync(dataSource, groupId);
      await InsertActivityAsync(
         dataSource,
         selectedActivityId,
         DistantActivityDate,
         ToUtc(DistantActivityDate),
         activityGroupId: groupId
      );
      await InsertActivityAsync(
         dataSource,
         futureActivityId,
         futureDate,
         ToUtc(futureDate),
         activityGroupId: groupId
      );
      await InsertActivityAsync(
         dataSource,
         pastActivityId,
         pastDate,
         ToUtc(pastDate),
         activityGroupId: groupId
      );
      await InsertActivityAsync(
         dataSource,
         unrelatedActivityId,
         futureDate,
         ToUtc(futureDate)
      );

      var activityIds = new[]
      {
         selectedActivityId,
         futureActivityId,
         pastActivityId,
         unrelatedActivityId
      };

      foreach(var activityId in activityIds)
      {
         await InsertActivityEntityLinkAsync(
            dataSource,
            activityId,
            personId
         );
      }

      try
      {
         await repository.SetParticipantActiveAsync(
            selectedActivityId,
            personId,
            false,
            CancellationToken.None
         );

         Assert.False(
            await GetParticipantActiveAsync(
               dataSource,
               selectedActivityId,
               personId
            )
         );
         Assert.False(
            await GetParticipantActiveAsync(
               dataSource,
               futureActivityId,
               personId
            )
         );
         Assert.True(
            await GetParticipantActiveAsync(
               dataSource,
               pastActivityId,
               personId
            )
         );
         Assert.True(
            await GetParticipantActiveAsync(
               dataSource,
               unrelatedActivityId,
               personId
            )
         );

         await repository.SetParticipantActiveAsync(
            pastActivityId,
            personId,
            false,
            CancellationToken.None
         );
         await repository.SetParticipantActiveAsync(
            selectedActivityId,
            personId,
            true,
            CancellationToken.None
         );

         Assert.True(
            await GetParticipantActiveAsync(
               dataSource,
               selectedActivityId,
               personId
            )
         );
         Assert.True(
            await GetParticipantActiveAsync(
               dataSource,
               futureActivityId,
               personId
            )
         );
         Assert.False(
            await GetParticipantActiveAsync(
               dataSource,
               pastActivityId,
               personId
            )
         );
      }
      finally
      {
         foreach(var activityId in activityIds)
         {
            await DeleteActivityEntityLinksAsync(dataSource, activityId);
            await DeleteActivityAsync(dataSource, activityId);
         }

         await DeleteActivityGroupAsync(dataSource, groupId);
         await DeleteEntityAsync(dataSource, personId);
      }
   }

   private static async Task InsertActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      DateOnly activityDate,
      DateTimeOffset startsAt,
      string publicationStatus = ActivityPublicationStatusIds.Draft,
      Guid? activityGroupId = null
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = $$"""
         insert into activities (
            id,
            title,
            description,
            teaser,
            activity_type_id,
            sport_id,
            activity_date,
            local_start_time,
            starts_at,
            time_zone_id,
            publication_status_id,
            tv_channel_name,
            slug,
            activity_group_id,
            published_at
         )
         values (
            @id,
            'Test Activity',
            null,
            null,
            'Match',
            'football',
            @activity_date,
            @local_start_time,
            @starts_at,
            'Europe/Stockholm',
            @publication_status,
            null,
            @slug,
            @activity_group_id,
            case
               when @publication_status =
                  '{{ActivityPublicationStatusIds.Published}}'
               then now()
               else null
            end
         )
         """;
      command.Parameters.AddWithValue("id", activityId);
      command.Parameters.AddWithValue("activity_date", activityDate);
      command.Parameters.AddWithValue(
         "local_start_time",
         startsAt.ToLocalTime().TimeOfDay
      );
      command.Parameters.AddWithValue("starts_at", startsAt);
      command.Parameters.AddWithValue(
         "publication_status",
         publicationStatus
      );
      command.Parameters.AddWithValue(
         "slug",
         $"test-activity-{activityId:N}"
      );
      command.Parameters.AddWithValue(
         "activity_group_id",
         (object?)activityGroupId ?? DBNull.Value
      );

      await command.ExecuteNonQueryAsync();
   }

   private static DateTimeOffset ToUtc(DateOnly date)
   {
      return TimeZoneHelper.ToUtc(
         date,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
   }

   private static async Task InsertActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into activity_groups (
            id,
            title,
            sport_id,
            start_date,
            end_date
         )
         values (
            @id,
            'Test Activity Group',
            'football',
            @start_date,
            @end_date
         )
         """
      );
      command.Parameters.AddWithValue("id", activityGroupId);
      command.Parameters.AddWithValue(
         "start_date",
         new DateOnly(1900, 1, 1)
      );
      command.Parameters.AddWithValue(
         "end_date",
         DistantActivityDate.AddDays(1)
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task<bool> GetParticipantActiveAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      Guid entityId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         select is_active
         from activity_entity_links
         where activity_id = @activity_id
            and entity_id = @entity_id
         """
      );
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);
      return (bool)(await command.ExecuteScalarAsync())!;
   }

   private static async Task InsertEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId,
      string entityName,
      string entityTypeId,
      string watchPriorityId = "tier_3",
      string? aliasName = null
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
            expected_stability_id,
            alias_name
         )
         values (
            @id,
            @canonical_name,
            @entity_type_id,
            'football',
            @country_id,
            'NationalityOrSportingIdentity',
            'Test coverage',
            @watch_priority_id,
            'short_term',
            @alias_name
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
      command.Parameters.AddWithValue("entity_type_id", entityTypeId);
      command.Parameters.AddWithValue("watch_priority_id", watchPriorityId);
      command.Parameters.AddWithValue(
         "alias_name",
         (object?)aliasName ?? DBNull.Value
      );

      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertActivityEntityLinkAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      Guid entityId,
      Guid? organizationEntityId = null
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            organization_entity_id
         )
         values (
            @id,
            @activity_id,
            @entity_id,
            @organization_entity_id
         )
         """;
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId ?? (object)DBNull.Value
      );

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
      Guid activityId
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
            input_payload,
            rendered_prompt,
            started_at,
            completed_at
         )
         values (
            @id,
            @job_id,
            @prompt_id,
            @provider_id,
            'completed',
            @correlation_id,
            '{}'::jsonb,
            'Rendered',
            now(),
            now()
         )
         """;
      command.Parameters.AddWithValue("id", runId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("prompt_id", promptId);
      command.Parameters.AddWithValue("provider_id", providerId);
      command.Parameters.AddWithValue("correlation_id", activityId.ToString());

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from activities
         where id = @id
         """;
      command.Parameters.AddWithValue("id", activityId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteRunAsync(
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

   private static async Task DeleteParticipantAiSourcesAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
            and kind = @kind
         """;
      command.Parameters.AddWithValue(
         "correlation_type",
         SourceCorrelationTypes.Activity
      );
      command.Parameters.AddWithValue(
         "correlation_id",
         activityId.ToString()
      );
      command.Parameters.AddWithValue(
         "kind",
         SourceKinds.ParticipantStartEvidence
      );

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         delete from activity_groups
         where id = @id
         """
      );
      command.Parameters.AddWithValue("id", activityGroupId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteActivityEntityLinksAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from activity_entity_links
         where activity_id = @id
         """;
      command.Parameters.AddWithValue("id", activityId);

      await command.ExecuteNonQueryAsync();
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

   private static async Task<JobContext> LoadJobContextAsync(
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

   private static string? InvokeGetSportIconPath(string? iconId)
   {
      var method = typeof(ActivityRepository).GetMethod(
         "GetSportIconPath",
         BindingFlags.NonPublic | BindingFlags.Static
      );

      if(method is null)
      {
         throw new InvalidOperationException(
            "Could not find GetSportIconPath."
         );
      }

      return (string?)method.Invoke(null, [iconId]);
   }

   private sealed record JobContext(
      string ProviderId,
      Guid PromptId
   );
}
