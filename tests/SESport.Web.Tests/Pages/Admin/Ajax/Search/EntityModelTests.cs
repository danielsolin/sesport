using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using Npgsql;

using SESport.Core.Configuration;
using SESport.Data;
using SESport.Web.Pages.Admin.Ajax.Search;

namespace SESport.Core.Tests.Pages.Admin.Ajax.Search;

public sealed class EntityModelTests
{
   [Fact]
   public async Task OnGetAsyncReturnsMatchingEntities()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var queryToken = Guid.NewGuid().ToString("N")[..8];
      var personToken = Guid.NewGuid().ToString("N")[..8];

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
         $"Person {personToken}",
         TrackedEntityTypeIds.Person,
         "football"
      );

      try
      {
         var result = await model.OnGetAsync(
            queryToken,
            null,
            CancellationToken.None,
            true
         );

         var jsonResult = Assert.IsType<JsonResult>(result);
         using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(jsonResult.Value)
         );
         var results = document.RootElement.GetProperty("results");

         Assert.Single(results.EnumerateArray());
         Assert.Contains(
            organizationId.ToString(),
            results.ToString()
         );
         Assert.Contains(
            $"Organization {queryToken}",
            results.ToString()
         );
         Assert.DoesNotContain(
            personId.ToString(),
            results.ToString()
         );
         Assert.DoesNotContain(
            $"Person {personToken}",
            results.ToString()
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnGetAsyncOrganizationOnlyExcludesPersonAndPair()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var pairId = Guid.NewGuid();
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
      await InsertRelatedEntityAsync(
         dataSource,
         pairId,
         $"Pair {queryToken}",
         TrackedEntityTypeIds.Pair,
         "football"
      );

      try
      {
         var result = await model.OnGetAsync(
            queryToken,
            null,
            CancellationToken.None,
            sortAsc: true,
            organizationOnly: true
         );

         var jsonResult = Assert.IsType<JsonResult>(result);
         using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(jsonResult.Value)
         );
         var results = document.RootElement.GetProperty("results");

         Assert.Single(results.EnumerateArray());
         Assert.Contains(
            organizationId.ToString(),
            results.ToString()
         );
         Assert.DoesNotContain(
            personId.ToString(),
            results.ToString()
         );
         Assert.DoesNotContain(
            pairId.ToString(),
            results.ToString()
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, pairId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnGetAsyncOrganizationOnlySearchesAliasName()
   {
      var organizationId = Guid.NewGuid();
      var queryToken = Guid.NewGuid().ToString("N")[..8];
      var aliasToken = Guid.NewGuid().ToString("N")[..8];

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var model = new EntityModel(repository);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Global Champions Tour {queryToken}",
         TrackedEntityTypeIds.Tour,
         "equestrian",
         $"Horse alias {aliasToken}"
      );

      try
      {
         var result = await model.OnGetAsync(
            aliasToken,
            null,
            CancellationToken.None,
            sortAsc: true,
            organizationOnly: true
         );

         var jsonResult = Assert.IsType<JsonResult>(result);
         using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(jsonResult.Value)
         );
         var results = document.RootElement.GetProperty("results");

         Assert.Single(results.EnumerateArray());
         Assert.Contains(
            organizationId.ToString(),
            results.ToString()
         );
         Assert.Contains(
            $"Global Champions Tour {queryToken}",
            results.ToString()
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnGetAsyncReturnsEmptyResultsForBlankTerm()
   {
      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var model = new EntityModel(repository);

      var result = await model.OnGetAsync(
         string.Empty,
         null,
         CancellationToken.None,
         true
      );

      var jsonResult = Assert.IsType<JsonResult>(result);
      using var document = JsonDocument.Parse(
         JsonSerializer.Serialize(jsonResult.Value)
      );
      var results = document.RootElement.GetProperty("results");

      Assert.Empty(results.EnumerateArray());
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
      string sportId,
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
            @sport_id,
            'se',
            'NationalityOrSportingIdentity',
            'Test coverage',
            'tier_3',
            'short_term',
            @alias_name
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
      command.Parameters.AddWithValue("entity_type_id", entityTypeId);
      command.Parameters.AddWithValue("sport_id", sportId);
      command.Parameters.AddWithValue(
         "alias_name",
         (object?)aliasName ?? DBNull.Value
      );

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
