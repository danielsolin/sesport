using Npgsql;
using SESport.Data.Broadcast;

namespace SESport.Core.Tests.Data;

public sealed class BroadcastRepositoryTests
{
   [Fact]
   public async Task HideIgnoredBroadcastsAsyncHidesMatchingBroadcasts()
   {
      var broadcastId = Guid.NewGuid();
      var ruleId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var categoryValue = $"Baseboll Test {Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");

      await using var dataSource = CreateDataSource();
      var repository = new BroadcastRepository(dataSource);

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         "Test Match",
         [categoryValue],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );
      await InsertIgnoreRuleAsync(
         dataSource,
         ruleId,
         "category_contains",
         categoryValue.ToLowerInvariant(),
         sourceKey
      );

      try
      {
         var hiddenCount = await repository.HideIgnoredBroadcastsAsync(
            sourceKey,
            CancellationToken.None
         );

         Assert.Equal(1, hiddenCount);

         await using var connection = await dataSource.OpenConnectionAsync();
         await using var command = connection.CreateCommand();
         command.CommandText = """
            select hidden_at
            from broadcasts
            where id = @id
            """;
         command.Parameters.AddWithValue("id", broadcastId);

         var hiddenAt = await command.ExecuteScalarAsync();

         Assert.NotNull(hiddenAt);
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteIgnoreRuleAsync(dataSource, ruleId);
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

   private static async Task InsertIgnoreRuleAsync(
      NpgsqlDataSource dataSource,
      Guid ruleId,
      string kind,
      string value,
      string? sourceKey
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into broadcast_ignore (
            id,
            kind,
            value,
            source_key,
            reason,
            is_active
         )
         values (
            @id,
            @kind,
            @value,
            @source_key,
            'Test coverage',
            true
         )
         """;
      command.Parameters.AddWithValue("id", ruleId);
      command.Parameters.AddWithValue("kind", kind);
      command.Parameters.AddWithValue("value", value);
      command.Parameters.AddWithValue(
         "source_key",
         (object?)sourceKey ?? DBNull.Value
      );

      await command.ExecuteNonQueryAsync();
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

   private static async Task DeleteIgnoreRuleAsync(
      NpgsqlDataSource dataSource,
      Guid ruleId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from broadcast_ignore
         where id = @id
         """;
      command.Parameters.AddWithValue("id", ruleId);

      await command.ExecuteNonQueryAsync();
   }
}
