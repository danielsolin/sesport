using Npgsql;
using SESport.Data;

namespace SESport.Core.Tests.Data;

public sealed class AdminRepositoryTests
{
   [Fact]
   public async Task GetPersonEntityNameOptionsAsyncReturnsInsertedPerson()
   {
      var entityId = Guid.NewGuid();
      var entityName = $"Test Person {entityId:N}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertEntityAsync(dataSource, entityId, entityName);

      try
      {
         var options = await repository.GetPersonEntityNameOptionsAsync(
            CancellationToken.None
         );

         Assert.Contains(
            options,
            option =>
               option.Id == entityId &&
               option.Name == entityName
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, entityId);
      }
   }

   private static NpgsqlDataSource CreateDataSource()
   {
      var connectionString =
         Environment.GetEnvironmentVariable("ConnectionStrings__Default") ??
         "Host=localhost;Port=5432;Database=sesport;" +
         "Username=sesport;Password=sesport";

      return new NpgsqlDataSourceBuilder(connectionString).Build();
   }

   private static async Task InsertEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId,
      string entityName
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
            'Person',
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
