using Microsoft.AspNetCore.Mvc;

using Npgsql;

using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Data;
using SESport.Web.Pages.Admin.Ajax.Update;

namespace SESport.Core.Tests.Pages.Admin.Ajax.Update;

public sealed class EntityLinkModelTests
{
   [Fact]
   public async Task OnPostAsyncAddsAndRemovesEntityLinks()
   {
      var sourceEntityId = Guid.NewGuid();
      var targetEntityId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var model = new EntityLinkModel(repository);

      await InsertRelatedEntityAsync(
         dataSource,
         sourceEntityId,
         $"Source {sourceEntityId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         targetEntityId,
         $"Target {targetEntityId:N}",
         TrackedEntityTypeIds.Person,
         "football"
      );

      try
      {
         var addResult = await model.OnPostAsync(
            sourceEntityId,
            "add",
            targetEntityId,
            CancellationToken.None
         );

         var addPayload = Assert.IsType<JsonResult>(addResult).Value!;

         Assert.True(GetRequiredProperty<bool>(addPayload, "updated"));
         Assert.Equal(
            "add",
            GetRequiredProperty<string>(addPayload, "action")
         );
         Assert.Equal(
            1,
            await CountEntityLinksAsync(
               dataSource,
               sourceEntityId,
               targetEntityId
            )
         );

         var removeResult = await model.OnPostAsync(
            sourceEntityId,
            "remove",
            targetEntityId,
            CancellationToken.None
         );

         var removePayload = Assert.IsType<JsonResult>(removeResult).Value!;

         Assert.True(GetRequiredProperty<bool>(removePayload, "updated"));
         Assert.Equal(
            "remove",
            GetRequiredProperty<string>(removePayload, "action")
         );
         Assert.Equal(
            0,
            await CountEntityLinksAsync(
               dataSource,
               sourceEntityId,
               targetEntityId
            )
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, sourceEntityId);
         await DeleteEntityAsync(dataSource, targetEntityId);
      }
   }

   private static T GetRequiredProperty<T>(object value, string name)
   {
      var property = value.GetType().GetProperty(name);

      Assert.NotNull(property);

      return Assert.IsType<T>(property!.GetValue(value));
   }

   private static NpgsqlDataSource CreateDataSource()
   {
      var connectionString = PostgresConnectionStrings.ResolveDefault();

      return new NpgsqlDataSourceBuilder(connectionString).Build();
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

   private static async Task<int> CountEntityLinksAsync(
      NpgsqlDataSource dataSource,
      Guid firstEntityId,
      Guid secondEntityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         select count(*)::int
         from entity_to_entity_links
         where (
               source_entity_id = @first_entity_id
               and target_entity_id = @second_entity_id
            )
            or (
               source_entity_id = @second_entity_id
               and target_entity_id = @first_entity_id
            )
         """;
      command.Parameters.AddWithValue("first_entity_id", firstEntityId);
      command.Parameters.AddWithValue("second_entity_id", secondEntityId);

      return (int)(await command.ExecuteScalarAsync())!;
   }
}
