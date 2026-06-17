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
      string entityTypeId
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
            'football',
            'se',
            'NationalityOrSportingIdentity',
            'Test coverage',
            'review',
            'short_term'
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
      command.Parameters.AddWithValue("entity_type_id", entityTypeId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertActivityEntityLinkAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      Guid entityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id
         )
         values (
            @id,
            @activity_id,
            @entity_id
         )
         """;
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);

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
