using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Npgsql;

using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Data;
using SESport.Web.Pages.Admin.Ajax.Search;

namespace SESport.Core.Tests.Pages.Admin.Ajax.Search;

public sealed class EntityModelTests
{
   [Fact]
   public async Task OnGetAsyncOrganizationOnlyReturnsOnlyOrganizations()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var queryToken = Guid.NewGuid().ToString("N")[..8];

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var model = new EntityModel(repository);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {queryToken}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {queryToken}",
         TrackedEntityTypeIds.Person,
         "football"
      );

      try
      {
         var result = await model.OnGetAsync(
            queryToken,
            true,
            CancellationToken.None
         );

         var jsonResult = Assert.IsType<JsonResult>(result);
         var json = JsonSerializer.Serialize(jsonResult.Value);

         Assert.Contains(organizationId.ToString(), json);
         Assert.Contains($"Organization {queryToken}", json);
         Assert.DoesNotContain(personId.ToString(), json);
         Assert.DoesNotContain($"Person {queryToken}", json);
      }
      finally
      {
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
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
}
