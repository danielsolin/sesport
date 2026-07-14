using Microsoft.AspNetCore.Mvc;

using Npgsql;

using SESport.Core.Configuration;
using SESport.Data;
using SESport.Web.Pages.Admin.Ajax.Update;

namespace SESport.Core.Tests.Pages.Admin.Ajax.Update;

public sealed class BroadcastFieldModelTests
{
   [Fact]
   public async Task OnPostAsyncUpdatesOrganizationAndRejectsInvalidType()
   {
      var broadcastId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var broadcastTitle = $"Broadcast {uniqueSuffix}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var model = new BroadcastFieldModel(repository, adminRepository);

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         broadcastTitle,
         ["Old", "Categories"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );
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

      try
      {
         var updateResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            organizationId.ToString(),
            CancellationToken.None
         );

         var updatePayload = Assert.IsType<JsonResult>(updateResult).Value!;
         var groupText = (string?)updatePayload.GetType()
            .GetProperty("groupText")
            ?.GetValue(updatePayload);

         Assert.Equal($"{broadcastTitle} (new)", groupText);
         AssertBroadcastEntityId(
            dataSource,
            broadcastId,
            organizationId
         );

         var invalidResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            personId.ToString(),
            CancellationToken.None
         );

         Assert.IsType<BadRequestObjectResult>(invalidResult);
         AssertBroadcastEntityId(
            dataSource,
            broadcastId,
            organizationId
         );

         var clearResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            null,
            CancellationToken.None
         );

         Assert.IsType<JsonResult>(clearResult);
         AssertBroadcastEntityId(dataSource, broadcastId, null);
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   private static NpgsqlDataSource CreateDataSource()
   {
      var connectionString = PostgresConnectionStrings.ResolveDefault();

      return new NpgsqlDataSourceBuilder(connectionString).Build();
   }

   private static async Task InsertBroadcastAsync(
      NpgsqlDataSource dataSource,
      Guid broadcastId,
      string sourceKey,
      string externalId,
      string fingerprint,
      string channelId,
      string channelName,
      string title,
      string[] categories,
      DateTimeOffset startsAt,
      DateTimeOffset endsAt
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
            raw_programme_xml
         )
         values (
            @id,
            @source_key,
            @external_id,
            @fingerprint,
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
            null
         )
         """;
      command.Parameters.AddWithValue("id", broadcastId);
      command.Parameters.AddWithValue("source_key", sourceKey);
      command.Parameters.AddWithValue("external_id", externalId);
      command.Parameters.AddWithValue("fingerprint", fingerprint);
      command.Parameters.AddWithValue("channel_id", channelId);
      command.Parameters.AddWithValue("channel_name", channelName);
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue("categories", categories);
      command.Parameters.AddWithValue("starts_at", startsAt);
      command.Parameters.AddWithValue("ends_at", endsAt);

      await command.ExecuteNonQueryAsync();
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
            'review',
            'short_term'
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
      command.Parameters.AddWithValue("entity_type_id", entityTypeId);
      command.Parameters.AddWithValue("sport_id", sportId);

      await command.ExecuteNonQueryAsync();
   }

   private static void AssertBroadcastEntityId(
      NpgsqlDataSource dataSource,
      Guid broadcastId,
      Guid? expectedEntityId
   )
   {
      using var connection = dataSource.OpenConnection();
      using var command = connection.CreateCommand();
      command.CommandText = """
         select entity_id
         from broadcasts
         where id = @id
         """;
      command.Parameters.AddWithValue("id", broadcastId);

      var actualValue = command.ExecuteScalar();

      if(expectedEntityId is null)
      {
         Assert.True(actualValue is null || actualValue is DBNull);
         return;
      }

      Assert.Equal(
         expectedEntityId,
         actualValue is Guid actualGuid ? actualGuid : null
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
}
