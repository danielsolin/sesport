using Npgsql;
using SESport.Core.Broadcast;
using CoreBroadcast = SESport.Core.Broadcast.Broadcast;

namespace SESport.Data.Broadcast;

public sealed class BroadcastRepository : IAsyncDisposable
{
   private readonly NpgsqlDataSource dataSource;
   private readonly bool ownsDataSource;

   public BroadcastRepository(NpgsqlDataSource dataSource)
   {
      this.dataSource = dataSource;
   }

   private BroadcastRepository(
      NpgsqlDataSource dataSource,
      bool ownsDataSource
   )
   {
      this.dataSource = dataSource;
      this.ownsDataSource = ownsDataSource;
   }

   public static BroadcastRepository Connect(string connectionString)
   {
      return new BroadcastRepository(
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

   public async Task<BroadcastSaveResult> SaveAsync(
      BroadcastImportRun importRun,
      IReadOnlyCollection<CoreBroadcast> broadcasts,
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

      var insertedCount = 0;
      var updatedCount = 0;

      foreach(var broadcast in broadcasts)
      {
         var inserted = await UpsertBroadcastAsync(
            connection,
            transaction,
            importRun.Id,
            broadcast,
            cancellationToken
         );

         if(inserted)
         {
            insertedCount++;
            continue;
         }

         updatedCount++;
      }

      await transaction.CommitAsync(cancellationToken);
      return new BroadcastSaveResult(
         broadcasts.Count,
         insertedCount,
         updatedCount
      );
   }

   public async Task<
      IReadOnlyCollection<BroadcastIgnoreRule>
   > GetIgnoreRulesAsync(
      string sourceKey,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select kind, value, source_key
         from broadcast_ignore
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
      var rules = new List<BroadcastIgnoreRule>();

      while(await reader.ReadAsync(cancellationToken))
      {
         rules.Add(
            new BroadcastIgnoreRule(
               reader.GetString(0),
               reader.GetString(1),
               reader.IsDBNull(2) ? null : reader.GetString(2)
            )
         );
      }

      return rules;
   }

   public async Task<int> HideIgnoredBroadcastsAsync(
      string sourceKey,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update broadcasts as broadcast
         set hidden_at = coalesce(hidden_at, now()),
            updated_at = now()
         where hidden_at is null
           and exists (
              select 1
              from broadcast_ignore as rule
              where rule.is_active = true
                and (rule.source_key is null or rule.source_key = @source_key)
                and (
                   (
                      rule.kind = 'channel_name'
                      and (
                         broadcast.channel_name = rule.value
                         or replace(
                            broadcast.channel_name,
                            '\u0026',
                            '&'
                         ) = rule.value
                         or regexp_replace(
                            replace(broadcast.channel_name, '\u0026', '&'),
                            '^SE - ',
                            ''
                         ) = rule.value
                      )
                   )
                   or (
                      rule.kind = 'category_contains'
                      and exists (
                         select 1
                         from unnest(broadcast.categories) as category
                         where category ilike '%' || rule.value || '%'
                      )
                   )
                )
           )
         """;

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var command = new NpgsqlCommand(sql, connection);
      command.Parameters.AddWithValue("source_key", sourceKey);

      return await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task UpsertImportRunAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      BroadcastImportRun importRun,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into broadcast_import_runs (
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

   private static async Task<bool> UpsertBroadcastAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid importRunId,
      CoreBroadcast broadcast,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         merge into broadcasts as target
         using (
            values (
               @id::uuid,
               @import_run_id::uuid,
               @source_key::text,
               @external_id::text,
               @fingerprint::text,
               @channel_id::text,
               @channel_name::text,
               @title::text,
               @description::text,
               @categories::text[],
               @is_replay::boolean,
               @original_air_date::date,
               @starts_at::timestamptz,
               @ends_at::timestamptz,
               @time_zone_id::text,
               @raw_programme_xml::text
            )
         ) as source (
            id, import_run_id, source_key, external_id, fingerprint,
            channel_id, channel_name, title, description, categories,
            is_replay, original_air_date, starts_at, ends_at, time_zone_id,
            raw_programme_xml
         )
         on target.fingerprint = source.fingerprint
         when matched then
            update set
               import_run_id = source.import_run_id,
               source_key = source.source_key,
               external_id = source.external_id,
               channel_id = source.channel_id,
               channel_name = source.channel_name,
               title = source.title,
               description = source.description,
               categories = case
                  when cardinality(source.categories) = 0
                     then target.categories
                  else source.categories
               end,
               is_replay = source.is_replay,
               original_air_date = source.original_air_date,
               starts_at = source.starts_at,
               ends_at = source.ends_at,
               time_zone_id = source.time_zone_id,
               raw_programme_xml = source.raw_programme_xml,
               updated_at = now()
         when not matched then
            insert (
               id, import_run_id, source_key, external_id, fingerprint,
               channel_id, channel_name, title, description, categories,
               is_replay, original_air_date, starts_at, ends_at, time_zone_id,
               raw_programme_xml
            )
            values (
               source.id, source.import_run_id, source.source_key,
               source.external_id, source.fingerprint, source.channel_id,
               source.channel_name, source.title, source.description,
               source.categories, source.is_replay, source.original_air_date,
               source.starts_at, source.ends_at, source.time_zone_id,
               source.raw_programme_xml
            )
         returning merge_action()
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

      var action = (string)(await command.ExecuteScalarAsync(
         cancellationToken
      ))!;
      return action.Equals("INSERT", StringComparison.OrdinalIgnoreCase);
   }

}

public sealed record BroadcastIgnoreRule(
   string Kind,
   string Value,
   string? SourceKey
);

public sealed record BroadcastSaveResult(
   int SavedCount,
   int InsertedCount,
   int UpdatedCount
);
