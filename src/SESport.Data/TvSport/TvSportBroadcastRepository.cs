using Npgsql;

using SESport.Core.TvSport;

namespace SESport.Data.TvSport;

public sealed class TvSportBroadcastRepository : IAsyncDisposable
{
   private readonly NpgsqlDataSource dataSource;
   private readonly bool ownsDataSource;

   public TvSportBroadcastRepository(NpgsqlDataSource dataSource)
   {
      this.dataSource = dataSource;
   }

   private TvSportBroadcastRepository(
      NpgsqlDataSource dataSource,
      bool ownsDataSource
   )
   {
      this.dataSource = dataSource;
      this.ownsDataSource = ownsDataSource;
   }

   public static TvSportBroadcastRepository Connect(string connectionString)
   {
      return new TvSportBroadcastRepository(
         NpgsqlDataSource.Create(connectionString),
         ownsDataSource: true
      );
   }

   public async ValueTask DisposeAsync()
   {
      if(ownsDataSource)
      {
         await dataSource.DisposeAsync();
      }
   }

   public async Task<int> SaveAsync(
      TvSportImportRun importRun,
      IReadOnlyCollection<TvSportBroadcast> broadcasts,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      await UpsertImportRunAsync(
         connection,
         transaction,
         importRun,
         cancellationToken
      );

      foreach(var broadcast in broadcasts)
      {
         await UpsertBroadcastAsync(
            connection,
            transaction,
            importRun.Id,
            broadcast,
            cancellationToken
         );
      }

      await transaction.CommitAsync(cancellationToken);
      return broadcasts.Count;
   }

   public async Task<
      IReadOnlyCollection<TvSportIgnoreRule>
   > GetIgnoreRulesAsync(
      string sourceKey,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select kind, value, source_key
         from tv_sport_ignore
         where is_active = true
           and (source_key is null or source_key = @source_key)
         order by kind, value
         """;

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var command = new NpgsqlCommand(sql, connection);
      command.Parameters.AddWithValue("source_key", sourceKey);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var rules = new List<TvSportIgnoreRule>();

      while(await reader.ReadAsync(cancellationToken))
      {
         rules.Add(
            new TvSportIgnoreRule(
               reader.GetString(0),
               reader.GetString(1),
               reader.IsDBNull(2) ? null : reader.GetString(2)
            )
         );
      }

      return rules;
   }

   public async Task<int> DeleteIgnoredBroadcastsAsync(
      string sourceKey,
      IReadOnlyCollection<TvSportIgnoreRule> ignoreRules,
      CancellationToken cancellationToken
   )
   {
      var ignoredChannelNames = ignoreRules
         .Where(rule => rule.Kind.Equals(
            "channel_name",
            StringComparison.OrdinalIgnoreCase
         ))
         .Select(rule => rule.Value)
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray();

      if(ignoredChannelNames.Length == 0)
      {
         return 0;
      }

      const string sql = """
         delete from tv_sport_broadcasts
         where source_key = @source_key
           and (
              channel_name = any(@channel_names)
              or regexp_replace(channel_name, '^SE - ', '') =
                 any(@channel_names)
           )
         """;

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var command = new NpgsqlCommand(sql, connection);
      command.Parameters.AddWithValue("source_key", sourceKey);
      command.Parameters.AddWithValue("channel_names", ignoredChannelNames);

      return await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task UpsertImportRunAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      TvSportImportRun importRun,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into tv_sport_import_runs (
            id, source_key, source_uri, started_at, finished_at, status,
            broadcast_count
         )
         values (
            @id, @source_key, @source_uri, @started_at, @finished_at,
            @status, @broadcast_count
         )
         on conflict (id) do update
         set
            source_key = excluded.source_key,
            source_uri = excluded.source_uri,
            started_at = excluded.started_at,
            finished_at = excluded.finished_at,
            status = excluded.status,
            broadcast_count = excluded.broadcast_count
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", importRun.Id);
      command.Parameters.AddWithValue("source_key", importRun.SourceKey);
      command.Parameters.AddWithValue(
         "source_uri",
         (object?)importRun.SourceUri?.ToString() ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "started_at",
         importRun.StartedAt.ToUniversalTime()
      );
      command.Parameters.AddWithValue(
         "finished_at",
         (object?)importRun.FinishedAt?.ToUniversalTime() ?? DBNull.Value
      );
      command.Parameters.AddWithValue("status", importRun.Status.ToString());
      command.Parameters.AddWithValue(
         "broadcast_count",
         importRun.BroadcastCount
      );

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task UpsertBroadcastAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid importRunId,
      TvSportBroadcast broadcast,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into tv_sport_broadcasts (
            id, import_run_id, source_key, external_id, fingerprint,
            channel_id, channel_name, title, description, categories,
            is_replay, original_air_date, starts_at, ends_at, time_zone_id,
            raw_programme_xml
         )
         values (
            @id, @import_run_id, @source_key, @external_id, @fingerprint,
            @channel_id, @channel_name, @title, @description, @categories,
            @is_replay, @original_air_date, @starts_at, @ends_at,
            @time_zone_id, @raw_programme_xml
         )
         on conflict (fingerprint) do update
         set
            import_run_id = excluded.import_run_id,
            source_key = excluded.source_key,
            external_id = excluded.external_id,
            channel_id = excluded.channel_id,
            channel_name = excluded.channel_name,
            title = excluded.title,
            description = excluded.description,
            categories = excluded.categories,
            is_replay = excluded.is_replay,
            original_air_date = excluded.original_air_date,
            starts_at = excluded.starts_at,
            ends_at = excluded.ends_at,
            time_zone_id = excluded.time_zone_id,
            raw_programme_xml = excluded.raw_programme_xml,
            updated_at = now()
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", broadcast.Id);
      command.Parameters.AddWithValue("import_run_id", importRunId);
      command.Parameters.AddWithValue("source_key", broadcast.SourceKey);
      command.Parameters.AddWithValue("external_id", broadcast.ExternalId);
      command.Parameters.AddWithValue("fingerprint", broadcast.Fingerprint);
      command.Parameters.AddWithValue("channel_id", broadcast.ChannelId);
      command.Parameters.AddWithValue(
         "channel_name",
         (object?)broadcast.ChannelName ?? DBNull.Value
      );
      command.Parameters.AddWithValue("title", broadcast.Title);
      command.Parameters.AddWithValue(
         "description",
         (object?)broadcast.Description ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "categories",
         broadcast.Categories.ToArray()
      );
      command.Parameters.AddWithValue("is_replay", broadcast.IsReplay);
      command.Parameters.AddWithValue(
         "original_air_date",
         (object?)broadcast.OriginalAirDate ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "starts_at",
         broadcast.StartsAt.ToUniversalTime()
      );
      command.Parameters.AddWithValue(
         "ends_at",
         broadcast.EndsAt.ToUniversalTime()
      );
      command.Parameters.AddWithValue("time_zone_id", broadcast.TimeZoneId);
      command.Parameters.AddWithValue(
         "raw_programme_xml",
         (object?)broadcast.RawProgrammeXml ?? DBNull.Value
      );

      await command.ExecuteNonQueryAsync(cancellationToken);
   }
}

public sealed record TvSportIgnoreRule(
   string Kind,
   string Value,
   string? SourceKey
);
