using Npgsql;

using SESport.Core.Formatting;

namespace SESport.Core.Tests.Data;

public sealed class BroadcastReadRepositoryTests
{
   [Fact]
   public async Task GetNextUnprocessedAsyncAdvancesAfterAtomicUpdate()
   {
      var date = new DateOnly(2199, 12, 10);
      var firstId = Guid.NewGuid();
      var secondId = Guid.NewGuid();
      var hiddenId = Guid.NewGuid();
      var processedId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var firstStart = TimeZoneHelper.ToUtc(
         date,
         new TimeOnly(10, 0),
         SportDay.TimeZoneId
      );
      var secondStart = firstStart.AddHours(1);

      await using var dataSource = CreateDataSource();
      var repository = new BroadcastReadRepository(dataSource);

      try
      {
         await InsertBroadcastAsync(
            dataSource,
            firstId,
            sourceKey,
            "First title",
            firstStart,
            firstStart.AddHours(1)
         );
         await InsertBroadcastAsync(
            dataSource,
            secondId,
            sourceKey,
            "Second title",
            secondStart,
            secondStart.AddHours(1)
         );
         await InsertBroadcastAsync(
            dataSource,
            hiddenId,
            sourceKey,
            "Hidden title",
            firstStart.AddHours(2),
            firstStart.AddHours(3),
            hidden: true
         );
         await InsertBroadcastAsync(
            dataSource,
            processedId,
            sourceKey,
            "Processed title",
            firstStart.AddHours(3),
            firstStart.AddHours(4),
            processedAt: DateTimeOffset.UtcNow
         );

         var first = await repository.GetNextUnprocessedAsync(
            date,
            CancellationToken.None
         );

         Assert.NotNull(first);
         Assert.Equal(firstId, first!.Id);

         var updated = await repository.UpdateTextAndMarkProcessedAsync(
            firstId,
            "Updated title",
            "Updated description",
            CancellationToken.None
         );

         Assert.NotNull(updated);
         Assert.Equal("Updated title", updated!.Title);
         Assert.Equal("Updated description", updated.Description);
         Assert.Null(updated.HiddenAt);
         Assert.NotNull(updated.ProcessedAt);

         var second = await repository.GetNextUnprocessedAsync(
            date,
            CancellationToken.None
         );
         Assert.NotNull(second);
         Assert.Equal(secondId, second!.Id);

         var repeatedUpdate =
            await repository.UpdateTextAndMarkProcessedAsync(
               firstId,
               "Should not be applied",
               null,
               CancellationToken.None
            );
         Assert.Null(repeatedUpdate);
      }
      finally
      {
         await DeleteBroadcastsAsync(
            dataSource,
            [firstId, secondId, hiddenId, processedId]
         );
      }
   }

   private static async Task InsertBroadcastAsync(
      NpgsqlDataSource dataSource,
      Guid id,
      string sourceKey,
      string title,
      DateTimeOffset startsAt,
      DateTimeOffset endsAt,
      bool hidden = false,
      DateTimeOffset? processedAt = null
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
            raw_programme_xml,
            hidden_at,
            processed_at
         )
         values (
            @id,
            @source_key,
            @external_id,
            @fingerprint,
            'test-channel',
            'Test channel',
            @title,
            null,
            '{Test}',
            false,
            null,
            @starts_at,
            @ends_at,
            'Europe/Stockholm',
            null,
            @hidden_at,
            @processed_at
         )
         """;
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("source_key", sourceKey);
      command.Parameters.AddWithValue("external_id", $"external-{id:N}");
      command.Parameters.AddWithValue("fingerprint", $"fingerprint-{id:N}");
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue("starts_at", startsAt);
      command.Parameters.AddWithValue("ends_at", endsAt);
      command.Parameters.AddWithValue(
         "hidden_at",
         hidden ? DateTimeOffset.UtcNow : DBNull.Value
      );
      command.Parameters.AddWithValue(
         "processed_at",
         (object?)processedAt ?? DBNull.Value
      );

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteBroadcastsAsync(
      NpgsqlDataSource dataSource,
      IReadOnlyCollection<Guid> ids
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from broadcasts
         where id = any(@ids)
         """;
      command.Parameters.AddWithValue("ids", ids.ToArray());
      await command.ExecuteNonQueryAsync();
   }
}
