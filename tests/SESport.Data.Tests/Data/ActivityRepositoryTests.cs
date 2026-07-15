using Npgsql;

using SESport.Core.Configuration;
using SESport.Core.Formatting;
using SESport.Data;

namespace SESport.Core.Tests.Data;

public sealed class ActivityRepositoryTests
{
   [Fact]
   public async Task GetActivitiesAsyncIncludesSeriesOrganizations()
   {
      var now = DateTimeOffset.UtcNow;
      var selectedDate = SportDay.Today(now).StartDate;
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
      var now = DateTimeOffset.UtcNow;
      var selectedDate = SportDay.Today(now).StartDate;
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
      var now = DateTimeOffset.UtcNow;
      var selectedDate = SportDay.Today(now).StartDate;
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
      var now = DateTimeOffset.UtcNow;
      var selectedDate = SportDay.Today(now).StartDate;
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
   public async Task GetPublishedForDateAsyncOrdersParticipantsByWatchPriority()
   {
      var now = DateTimeOffset.UtcNow;
      var selectedDate = SportDay.Today(now).StartDate;
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
         startsAt
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
         "review"
      );
      await InsertEntityAsync(
         dataSource,
         reviewBravoId,
         "Bravo Review",
         TrackedEntityTypeIds.Person,
         "review"
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
         SportDay.Today(DateTimeOffset.UtcNow).StartDate,
         DateTimeOffset.UtcNow
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

   private static NpgsqlDataSource CreateDataSource()
   {
      var connectionString = PostgresConnectionStrings.ResolveDefault();

      return new NpgsqlDataSourceBuilder(connectionString).Build();
   }

   private static async Task InsertActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      DateOnly activityDate,
      DateTimeOffset startsAt
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
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
            slug
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
            'Published',
            null,
            @slug
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
         "slug",
         $"test-activity-{activityId:N}"
      );

      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId,
      string entityName,
      string entityTypeId,
      string watchPriorityId = "review",
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
            'se',
            'NationalityOrSportingIdentity',
            'Test coverage',
            @watch_priority_id,
            'short_term',
            @alias_name
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
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
}
