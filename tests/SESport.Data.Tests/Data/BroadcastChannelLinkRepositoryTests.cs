using Npgsql;

using SESport.Data.Repositories;

namespace SESport.Core.Tests.Data;

public sealed class BroadcastChannelLinkRepositoryTests
{
   [Fact]
   public async Task GetActiveDefinitionsAsyncReadsActiveRows()
   {
      var activeName = $"Test Channel {Guid.NewGuid():N}";
      var inactiveName = $"Inactive Channel {Guid.NewGuid():N}";
      await using var dataSource = CreateDataSource();

      try
      {
         await InsertAsync(
            dataSource,
            activeName,
            "test-link",
            ["Test Alias"],
            true
         );
         await InsertAsync(
            dataSource,
            inactiveName,
            "inactive-link",
            [],
            false
         );

         var repository = new BroadcastChannelLinkRepository(dataSource);
         var definitions = await repository.GetActiveDefinitionsAsync(
            CancellationToken.None
         );
         var definition = Assert.Single(
            definitions,
            item => item.CanonicalName == activeName
         );

         Assert.Equal("test-link", definition.Url);
         Assert.Equal(["Test Alias"], definition.Aliases);
         Assert.DoesNotContain(
            definitions,
            item => item.CanonicalName == inactiveName
         );
      }
      finally
      {
         await DeleteAsync(dataSource, activeName, inactiveName);
      }
   }

   private static async Task InsertAsync(
      NpgsqlDataSource dataSource,
      string canonicalName,
      string url,
      string[] aliases,
      bool isActive
   )
   {
      const string sql = """
         insert into broadcast_channel_links (
            canonical_name,
            url,
            aliases,
            is_active
         )
         values (@canonical_name, @url, @aliases, @is_active)
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("canonical_name", canonicalName);
      command.Parameters.AddWithValue("url", url);
      command.Parameters.AddWithValue("aliases", aliases);
      command.Parameters.AddWithValue("is_active", isActive);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteAsync(
      NpgsqlDataSource dataSource,
      string activeName,
      string inactiveName
   )
   {
      const string sql = """
         delete from broadcast_channel_links
         where canonical_name in (@active_name, @inactive_name)
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("active_name", activeName);
      command.Parameters.AddWithValue("inactive_name", inactiveName);
      await command.ExecuteNonQueryAsync();
   }
}
