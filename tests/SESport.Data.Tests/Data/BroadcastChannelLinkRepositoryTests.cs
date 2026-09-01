using Npgsql;

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

   [Fact]
   public async Task SaveAsyncInsertsAndUpdatesAndReadsActiveState()
   {
      var canonicalName = $"Test Link Channel {Guid.NewGuid():N}";
      await using var dataSource = CreateDataSource();

      try
      {
         var repository = new BroadcastChannelLinkRepository(dataSource);
         await repository.SaveAsync(
            null,
            canonicalName,
            "https://example.test/one",
            ["Alias One"],
            true,
            CancellationToken.None
         );

         var saved = await repository.GetByNameAsync(
            canonicalName,
            CancellationToken.None
         );

         Assert.NotNull(saved);
         Assert.Equal("https://example.test/one", saved!.Url);
         Assert.Equal(["Alias One"], saved.Aliases);
         Assert.True(saved.IsActive);

         await repository.SaveAsync(
            canonicalName,
            canonicalName,
            "https://example.test/two",
            ["Alias Two"],
            false,
            CancellationToken.None
         );

         var updated = await repository.GetByNameAsync(
            canonicalName,
            CancellationToken.None
         );

         Assert.NotNull(updated);
         Assert.Equal("https://example.test/two", updated!.Url);
         Assert.Equal(["Alias Two"], updated.Aliases);
         Assert.False(updated.IsActive);
         var row = Assert.Single(
            await repository.GetAllAsync(CancellationToken.None),
            item => item.CanonicalName == canonicalName
         );
         Assert.Equal("https://example.test/two", row.Url);
         Assert.False(row.IsActive);
         Assert.DoesNotContain(
            await repository.GetActiveDefinitionsAsync(
               CancellationToken.None
            ),
            item => item.CanonicalName == canonicalName
         );
      }
      finally
      {
         await DeleteAsync(dataSource, canonicalName);
      }
   }

   [Fact]
   public async Task GetByNameAsyncReturnsNullForMissingRow()
   {
      await using var dataSource = CreateDataSource();

      var result = await new BroadcastChannelLinkRepository(
         dataSource
      ).GetByNameAsync(
         $"Missing Channel {Guid.NewGuid():N}",
         CancellationToken.None
      );

      Assert.Null(result);
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
      params string[] canonicalNames
   )
   {
      foreach(var canonicalName in canonicalNames)
      {
         const string sql = """
            delete from broadcast_channel_links
            where canonical_name = @canonical_name
            """;

         await using var command = dataSource.CreateCommand(sql);
         command.Parameters.AddWithValue("canonical_name", canonicalName);
         await command.ExecuteNonQueryAsync();
      }
   }
}
