using Npgsql;

using SESport.Core.Broadcast;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Core.Tests.Data;

public sealed class BroadcastRepositoryTests
{
   [Fact]
   public async Task GetByDateAsyncFiltersByTitleCaseInsensitively()
   {
      var date = new DateOnly(2199, 12, 1);
      var startsAt = new DateTimeOffset(
         2199,
         12,
         1,
         12,
         0,
         0,
         TimeSpan.Zero
      );
      var endsAt = startsAt.AddHours(2);
      var broadcastIds = new[]
      {
         Guid.NewGuid(),
         Guid.NewGuid(),
         Guid.NewGuid()
      };
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var suffix = Guid.NewGuid().ToString("N");

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);

      try
      {
         await InsertBroadcastAsync(
            dataSource,
            broadcastIds[0],
            sourceKey,
            $"external-1-{suffix}",
            $"fingerprint-1-{suffix}",
            "channel-1",
            "Test channel",
            "Arsenal v Chelsea",
            ["Fotboll"],
            startsAt,
            endsAt
         );
         await InsertBroadcastAsync(
            dataSource,
            broadcastIds[1],
            sourceKey,
            $"external-2-{suffix}",
            $"fingerprint-2-{suffix}",
            "channel-2",
            "Test channel",
            "Liverpool v Arsenal",
            ["Fotboll"],
            startsAt,
            endsAt
         );
         await InsertBroadcastAsync(
            dataSource,
            broadcastIds[2],
            sourceKey,
            $"external-3-{suffix}",
            $"fingerprint-3-{suffix}",
            "channel-3",
            "Test channel",
            "Bayern Munich",
            ["Fotboll"],
            startsAt,
            endsAt
         );

         var broadcasts = await repository.GetByDateAsync(
            date,
            false,
            false,
            [],
            "arsenal",
            CancellationToken.None
         );

         Assert.Equal(2, broadcasts.Count);
         Assert.All(
            broadcasts,
            broadcast => Assert.Contains(
               "arsenal",
               broadcast.Title,
               StringComparison.OrdinalIgnoreCase
            )
         );
      }
      finally
      {
         foreach(var broadcastId in broadcastIds)
         {
            await DeleteBroadcastAsync(dataSource, broadcastId);
         }
      }
   }

   [Fact]
   public async Task HideIgnoredBroadcastsAsyncHidesMatchingBroadcasts()
   {
      var broadcastId = Guid.NewGuid();
      var ruleId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var categoryValue = $"Baseboll Test {Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");

      await using var dataSource = CreateDataSource();
      var repository = new BroadcastImportRepository(dataSource);

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

   [Fact]
   public async Task UpdateBroadcastTextFieldsAsyncUpdatesBroadcast()
   {
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         "Old title",
         ["Old", "Categories"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );

      try
      {
         await repository.UpdateTitleAsync(
            broadcastId,
            "New title",
            CancellationToken.None
         );
         await repository.UpdateCategoriesAsync(
            broadcastId,
            ["New", "Categories"],
            CancellationToken.None
         );

         await using var connection = await dataSource.OpenConnectionAsync();
         await using var command = connection.CreateCommand();
         command.CommandText = """
            select title, categories
            from broadcasts
            where id = @id
            """;
         command.Parameters.AddWithValue("id", broadcastId);

         await using var reader = await command.ExecuteReaderAsync();
         Assert.True(await reader.ReadAsync());
         Assert.Equal("New title", reader.GetString(0));
         Assert.Equal(
            ["New", "Categories"],
            reader.GetFieldValue<string[]>(1)
         );
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
      }
   }

   [Fact]
   public async Task SaveAsyncDoesNotClearCategoriesWhenSourceIsEmpty()
   {
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");

      await using var dataSource = CreateDataSource();
      var repository = new BroadcastImportRepository(dataSource);
      var existingCategories = new[] { "Handboll", "U20 VM" };
      var broadcast = new global::SESport.Core.Broadcast.Broadcast(
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         "Test Match",
         null,
         new string[0],
         false,
         null,
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2),
         "Europe/Stockholm",
         null,
         null
      );

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         "Test Match",
         existingCategories,
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );

      try
      {
         var importRun = new BroadcastImportRun(
            Guid.NewGuid(),
            sourceKey,
            new Uri("https://example.invalid/broadcasts"),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            BroadcastImportRunStatus.Completed,
            1
         );

         await repository.SaveAsync(
            importRun,
            [broadcast],
            CancellationToken.None
         );

         await using var connection = await dataSource.OpenConnectionAsync();
         await using var command = connection.CreateCommand();
         command.CommandText = """
            select categories
            from broadcasts
            where id = @id
            """;
         command.Parameters.AddWithValue("id", broadcastId);

         await using var reader = await command.ExecuteReaderAsync();
         Assert.True(await reader.ReadAsync());
         Assert.Equal(
            existingCategories,
            reader.GetFieldValue<string[]>(0)
         );
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
      }
   }

   [Fact]
   public async Task SaveAsyncRepairsLegacyFingerprintForExistingId()
   {
      var broadcastId = Guid.NewGuid();
      var importRunId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var legacyFingerprint = $"legacy-{uniqueSuffix}";
      var currentFingerprint = $"current-{uniqueSuffix}";
      var startsAt = new DateTimeOffset(
         2199,
         12,
         1,
         11,
         0,
         0,
         TimeSpan.Zero
      );
      var endsAt = startsAt.AddHours(2);

      await using var dataSource = CreateDataSource();
      var repository = new BroadcastImportRepository(dataSource);
      var broadcast = new global::SESport.Core.Broadcast.Broadcast(
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         currentFingerprint,
         "channel-1",
         "Viaplay",
         "Updated title",
         null,
         ["Test"],
         false,
         null,
         startsAt,
         endsAt,
         "Europe/Stockholm",
         null,
         null
      );

      try
      {
         await InsertBroadcastAsync(
            dataSource,
            broadcastId,
            sourceKey,
            $"external-{uniqueSuffix}",
            legacyFingerprint,
            "channel-1",
            "Viaplay",
            "Legacy title",
            ["Test"],
            startsAt,
            endsAt
         );

         var importRun = new BroadcastImportRun(
            importRunId,
            sourceKey,
            new Uri("https://example.invalid/broadcasts"),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            BroadcastImportRunStatus.Completed,
            1
         );

         var result = await repository.SaveAsync(
            importRun,
            [broadcast],
            CancellationToken.None
         );

         Assert.Equal(1, result.UpdatedCount);
         Assert.Equal(0, result.InsertedCount);

         await using var connection = await dataSource.OpenConnectionAsync();
         await using var command = connection.CreateCommand();
         command.CommandText = """
            select id, fingerprint, title
            from broadcasts
            where id = @id
            """;
         command.Parameters.AddWithValue("id", broadcastId);

         await using var reader = await command.ExecuteReaderAsync();
         Assert.True(await reader.ReadAsync());
         Assert.Equal(broadcastId, reader.GetGuid(0));
         Assert.Equal(currentFingerprint, reader.GetString(1));
         Assert.Equal("Updated title", reader.GetString(2));
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteImportRunAsync(dataSource, importRunId);
      }
   }

   [Fact]
   public async Task SaveAsyncPersistsOneCurrentStreamLinkPerProvider()
   {
      var broadcastId = Guid.NewGuid();
      var importRunId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var firstUrl =
         "https://max.prf.hn/click/camref:1011l3PaYv/" +
         $"pubref:test/destination:https://play.hbomax.com/" +
         $"video/watch/{uniqueSuffix}?utm_source=universal_search";
      var normalizedFirstUrl =
         $"https://play.hbomax.com/video/watch/" +
         $"{uniqueSuffix}";
      var secondUrl = $"https://stream.example/{uniqueSuffix}/second";
      var importRun = new BroadcastImportRun(
         importRunId,
         sourceKey,
         new Uri("https://example.invalid/broadcasts"),
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow,
         BroadcastImportRunStatus.Completed,
         1
      );
      var broadcast = new global::SESport.Core.Broadcast.Broadcast(
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "stream",
         "Viaplay",
         "Test stream",
         null,
         ["Fotboll"],
         false,
         null,
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2),
         "Europe/Stockholm",
         null,
         null
      )
      {
         StreamLinks = [new BroadcastStreamLink("Viaplay", firstUrl)]
      };

      await using var dataSource = CreateDataSource();
      var repository = new BroadcastImportRepository(dataSource);
      var sourceRepository = new SourceReferenceRepository(dataSource);

      try
      {
         await repository.SaveAsync(
            importRun,
            [broadcast],
            CancellationToken.None
         );

         var firstSources = await sourceRepository.GetByCorrelationAsync(
            SourceCorrelationTypes.Broadcast,
            broadcastId.ToString(),
            SourceKinds.StreamLink,
            CancellationToken.None
         );
         var firstSource = Assert.Single(firstSources);
         Assert.Equal("Viaplay", firstSource.Title);
         Assert.Equal(normalizedFirstUrl, firstSource.Url);

         await repository.SaveAsync(
            importRun,
            [broadcast with
            {
               StreamLinks = [new BroadcastStreamLink("Viaplay", secondUrl)]
            }],
            CancellationToken.None
         );

         var secondSources = await sourceRepository.GetByCorrelationAsync(
            SourceCorrelationTypes.Broadcast,
            broadcastId.ToString(),
            SourceKinds.StreamLink,
            CancellationToken.None
         );
         var secondSource = Assert.Single(secondSources);
         Assert.Equal(secondUrl, secondSource.Url);
      }
      finally
      {
         await sourceRepository.DeleteByCorrelationAsync(
            SourceCorrelationTypes.Broadcast,
            broadcastId.ToString(),
            CancellationToken.None
         );
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteImportRunAsync(dataSource, importRunId);
      }
   }

   [Fact]
   public async Task SaveAsyncUpdatesStreamLinkOnAlreadyLinkedActivity()
   {
      var broadcastId = Guid.NewGuid();
      var importRunId = Guid.NewGuid();
      Guid? activityId = null;
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var firstUrl = $"https://stream.example/{uniqueSuffix}/first";
      var secondUrl = $"https://stream.example/{uniqueSuffix}/second";
      var importRun = new BroadcastImportRun(
         importRunId,
         sourceKey,
         new Uri("https://example.invalid/broadcasts"),
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow,
         BroadcastImportRunStatus.Completed,
         1
      );
      var broadcast = new global::SESport.Core.Broadcast.Broadcast(
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "stream",
         "Viaplay",
         "Linked stream",
         null,
         ["Fotboll"],
         false,
         null,
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2),
         "Europe/Stockholm",
         null,
         null
      )
      {
         StreamLinks = [new BroadcastStreamLink("Viaplay", firstUrl)]
      };

      await using var dataSource = CreateDataSource();
      var broadcastRepository = new BroadcastImportRepository(dataSource);
      var activityRepository = new ActivityRepository(dataSource);
      var sourceRepository = new SourceReferenceRepository(dataSource);

      try
      {
         await broadcastRepository.SaveAsync(
            importRun,
            [broadcast],
            CancellationToken.None
         );

         activityId = await activityRepository.SaveAsync(
            new ActivityEditModel
            {
               Title = "Linked stream",
               ActivityType = "Match",
               SportId = "football",
               ActivityDate = DistantActivityDate,
               LocalStartTime = new TimeOnly(12, 0),
               LocalEndTime = new TimeOnly(14, 0),
               TimeZoneId = SportDay.TimeZoneId,
               IsPublished = true,
               BroadcastIds = [broadcastId]
            },
            CancellationToken.None
         );

         await broadcastRepository.SaveAsync(
            importRun,
            [broadcast with
            {
               StreamLinks = [new BroadcastStreamLink("Viaplay", secondUrl)]
            }],
            CancellationToken.None
         );

         var activitySources = await sourceRepository.GetByCorrelationAsync(
            SourceCorrelationTypes.Activity,
            activityId.Value.ToString(),
            SourceKinds.StreamLink,
            CancellationToken.None
         );
         var activitySource = Assert.Single(activitySources);
         Assert.Equal(secondUrl, activitySource.Url);
      }
      finally
      {
         if(activityId is not null)
         {
            await sourceRepository.DeleteByCorrelationAsync(
               SourceCorrelationTypes.Activity,
               activityId.Value.ToString(),
               CancellationToken.None
            );
            await DeleteActivityBroadcastLinksAsync(
               dataSource,
               activityId.Value
            );
            await DeleteActivityAsync(dataSource, activityId.Value);
         }

         await sourceRepository.DeleteByCorrelationAsync(
            SourceCorrelationTypes.Broadcast,
            broadcastId.ToString(),
            CancellationToken.None
         );
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteImportRunAsync(dataSource, importRunId);
      }
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

   private static async Task DeleteActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from activities
         where id = @id
         """;
      command.Parameters.AddWithValue("id", activityId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteActivityBroadcastLinksAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from activity_broadcast_links
         where activity_id = @activity_id
         """;
      command.Parameters.AddWithValue("activity_id", activityId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteImportRunAsync(
      NpgsqlDataSource dataSource,
      Guid importRunId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from broadcast_import_runs
         where id = @id
         """;
      command.Parameters.AddWithValue("id", importRunId);

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
