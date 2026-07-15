using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Npgsql;

using SESport.Data.Configuration;
using SESport.Data;
using SESport.Web.Pages.Admin.Entities;

namespace SESport.Core.Tests.Pages.Admin.Entities;

public sealed class IndexModelTests
{
   [Fact]
   public async Task OnGetAsyncUsesFilterCookieOnPageLoad()
   {
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var queryToken = Guid.NewGuid().ToString("N")[..8];

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var model = new IndexModel(repository)
      {
         PageContext = new PageContext
         {
            HttpContext = CreateContext(
               $"{IndexModel.FilterCookieName}=Organization"
            )
         }
      };

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
         await model.OnGetAsync(
            null,
            true,
            CancellationToken.None
         );

         Assert.Equal("Organization", model.Filter);
         Assert.True(model.HasFilter);
         Assert.Empty(model.Entities);
      }
      finally
      {
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnGetAsyncShowsNoEntitiesWithoutFilterCookie()
   {
      var entityId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var model = new IndexModel(repository)
      {
         PageContext = new PageContext
         {
            HttpContext = new DefaultHttpContext()
         }
      };

      await InsertRelatedEntityAsync(
         dataSource,
         entityId,
         $"Entity {Guid.NewGuid():N}",
         TrackedEntityTypeIds.Person,
         "football"
      );

      try
      {
         await model.OnGetAsync(
            null,
            true,
            CancellationToken.None
         );

         Assert.Equal(string.Empty, model.Filter);
         Assert.False(model.HasFilter);
         Assert.Empty(model.Entities);
      }
      finally
      {
         await DeleteEntityAsync(dataSource, entityId);
      }
   }

   private static DefaultHttpContext CreateContext(string cookieHeader)
   {
      var context = new DefaultHttpContext();
      context.Request.Headers.Cookie = cookieHeader;
      return context;
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
