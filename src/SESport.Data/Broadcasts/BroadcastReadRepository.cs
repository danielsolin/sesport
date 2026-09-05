using Npgsql;

using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Data.Broadcasts;

public sealed class BroadcastReadRepository(NpgsqlDataSource dataSource)
{
   private const string SelectSql = """
      select
         b.id,
         b.import_run_id,
         b.source_key,
         b.external_id,
         b.fingerprint,
         b.channel_id,
         b.channel_name,
         b.title,
         b.description,
         b.categories,
         b.is_replay,
         b.original_air_date,
         b.starts_at,
         b.ends_at,
         b.time_zone_id,
         b.raw_programme_xml,
         b.image_url,
         b.created_at,
         b.updated_at,
         b.hidden_at,
         b.processed_at,
         b.entity_id,
         organization.canonical_name,
         organization.sport_id,
         organization_sport.name,
         source_group.id,
         source_group.title,
         b.activity_group_draft_title,
         b.activity_group_source_kind_id,
         b.activity_group_source_activity_id
      from broadcasts b
      left join entities organization
         on organization.id = b.entity_id
      left join sports organization_sport
         on organization_sport.id = organization.sport_id
      left join activities source_activity
         on source_activity.id = b.activity_group_source_activity_id
      left join activity_groups source_group
         on source_group.id = source_activity.activity_group_id
      """;

   public async Task<BroadcastReadModel?> GetNextUnprocessedAsync(
      DateOnly date,
      CancellationToken cancellationToken
   )
   {
      var window = SportDay.ForDate(date);
      var start = TimeZoneHelper.ToUtc(
         window.StartDate,
         window.Cutoff,
         SportDay.TimeZoneId
      );
      var end = TimeZoneHelper.ToUtc(
         window.EndDateExclusive,
         window.Cutoff,
         SportDay.TimeZoneId
      );

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      var broadcast = await ReadOneAsync(
         connection,
         null,
         SelectSql + "\n" + """
            where b.starts_at >= @start
               and b.starts_at < @end
               and b.hidden_at is null
               and b.processed_at is null
            order by
               b.starts_at,
               b.channel_name nulls last,
               b.channel_id,
               b.title,
               b.id
            limit 1
            """,
         command =>
         {
            command.Parameters.AddWithValue("start", start);
            command.Parameters.AddWithValue("end", end);
         },
         cancellationToken
      );

      return broadcast is null
         ? null
         : await LoadRelationsAsync(
            connection,
            null,
            broadcast,
            cancellationToken
         );
   }

   public async Task<BroadcastReadModel?>
      UpdateTextAndMarkProcessedAsync(
         Guid id,
         string title,
         string? description,
         CancellationToken cancellationToken
      )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      const string updateSql = """
         update broadcasts
         set title = @title,
            description = @description,
            processed_at = now(),
            updated_at = now()
         where id = @id
            and hidden_at is null
            and processed_at is null
         returning id
         """;

      await using(var command = new NpgsqlCommand(
         updateSql,
         connection,
         transaction
      ))
      {
         command.Parameters.AddWithValue("id", id);
         command.Parameters.AddWithValue("title", title);
         command.Parameters.AddWithValue(
            "description",
            (object?)description ?? DBNull.Value
         );

         var updatedId = await command.ExecuteScalarAsync(
            cancellationToken
         );
         if(updatedId is null)
         {
            return null;
         }
      }

      var broadcast = await ReadByIdAsync(
         connection,
         transaction,
         id,
         cancellationToken
      );
      if(broadcast is null)
      {
         return null;
      }

      var result = await LoadRelationsAsync(
         connection,
         transaction,
         broadcast,
         cancellationToken
      );
      await transaction.CommitAsync(cancellationToken);
      return result;
   }

   private async Task<BroadcastReadModel?> ReadByIdAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid id,
      CancellationToken cancellationToken
   )
   {
      return await ReadOneAsync(
         connection,
         transaction,
         SelectSql + "\n" + """
            where b.id = @id
            """,
         command => command.Parameters.AddWithValue("id", id),
         cancellationToken
      );
   }

   private static async Task<BroadcastReadModel?> ReadOneAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction? transaction,
      string sql,
      Action<NpgsqlCommand> configure,
      CancellationToken cancellationToken
   )
   {
      await using var command = connection.CreateCommand();
      command.CommandText = sql;
      if(transaction is not null)
      {
         command.Transaction = transaction;
      }

      configure(command);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken)
         ? ReadBroadcast(reader)
         : null;
   }

   private static async Task<BroadcastReadModel> LoadRelationsAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction? transaction,
      BroadcastReadModel broadcast,
      CancellationToken cancellationToken
   )
   {
      var sources = await ReadSourcesAsync(
         connection,
         transaction,
         broadcast.Id,
         cancellationToken
      );
      var linkedActivityIds = await ReadLinkedActivityIdsAsync(
         connection,
         transaction,
         broadcast.Id,
         cancellationToken
      );

      return broadcast with
      {
         Sources = sources,
         LinkedActivityIds = linkedActivityIds
      };
   }

   private static async Task<IReadOnlyList<BroadcastReadSource>>
      ReadSourcesAsync(
         NpgsqlConnection connection,
         NpgsqlTransaction? transaction,
         Guid broadcastId,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            kind,
            url,
            title,
            excerpt,
            observed_at,
            created_at
         from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
         order by observed_at desc, created_at desc, id desc
         """;

      await using var command = connection.CreateCommand();
      command.CommandText = sql;
      if(transaction is not null)
      {
         command.Transaction = transaction;
      }

      command.Parameters.AddWithValue(
         "correlation_type",
         SourceCorrelationTypes.Broadcast
      );
      command.Parameters.AddWithValue(
         "correlation_id",
         broadcastId.ToString()
      );

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var sources = new List<BroadcastReadSource>();

      while(await reader.ReadAsync(cancellationToken))
      {
         sources.Add(
            new BroadcastReadSource(
               reader.GetString(0),
               reader.GetString(1),
               ReadString(reader, 2),
               ReadString(reader, 3),
               reader.GetFieldValue<DateTimeOffset>(4),
               reader.GetFieldValue<DateTimeOffset>(5)
            )
         );
      }

      return sources;
   }

   private static async Task<IReadOnlyList<Guid>>
      ReadLinkedActivityIdsAsync(
         NpgsqlConnection connection,
         NpgsqlTransaction? transaction,
         Guid broadcastId,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select activity_id
         from activity_broadcast_links
         where broadcast_id = @broadcast_id
         order by activity_id
         """;

      await using var command = connection.CreateCommand();
      command.CommandText = sql;
      if(transaction is not null)
      {
         command.Transaction = transaction;
      }

      command.Parameters.AddWithValue("broadcast_id", broadcastId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var activityIds = new List<Guid>();

      while(await reader.ReadAsync(cancellationToken))
      {
         activityIds.Add(reader.GetGuid(0));
      }

      return activityIds;
   }

   private static BroadcastReadModel ReadBroadcast(
      NpgsqlDataReader reader
   )
   {
      var organization = reader.IsDBNull(21)
         ? null
         : new BroadcastReadOrganization(
            reader.GetGuid(21),
            reader.GetString(22),
            ReadString(reader, 23),
            ReadString(reader, 24)
         );
      var hasActivityGroup = !reader.IsDBNull(25)
         || !reader.IsDBNull(26)
         || !reader.IsDBNull(27)
         || !reader.IsDBNull(28)
         || !reader.IsDBNull(29);
      var activityGroup = !hasActivityGroup
         ? null
         : new BroadcastReadActivityGroup(
            reader.IsDBNull(25) ? null : reader.GetGuid(25),
            ReadString(reader, 26),
            ReadString(reader, 27),
            ReadString(reader, 28),
            reader.IsDBNull(29) ? null : reader.GetGuid(29)
         );

      return new BroadcastReadModel(
         reader.GetGuid(0),
         reader.IsDBNull(1) ? null : reader.GetGuid(1),
         reader.GetString(2),
         reader.GetString(3),
         reader.GetString(4),
         reader.GetString(5),
         ReadString(reader, 6),
         reader.GetString(7),
         ReadString(reader, 8),
         reader.GetFieldValue<string[]>(9),
         reader.GetBoolean(10),
         reader.IsDBNull(11)
            ? null
            : reader.GetFieldValue<DateOnly>(11),
         reader.GetFieldValue<DateTimeOffset>(12),
         reader.GetFieldValue<DateTimeOffset>(13),
         reader.GetString(14),
         ReadString(reader, 15),
         ReadString(reader, 16),
         reader.GetFieldValue<DateTimeOffset>(17),
         reader.GetFieldValue<DateTimeOffset>(18),
         ReadDateTimeOffset(reader, 19),
         ReadDateTimeOffset(reader, 20),
         organization,
         activityGroup,
         [],
         []
      );
   }

   private static string? ReadString(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static DateTimeOffset? ReadDateTimeOffset(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<DateTimeOffset>(ordinal);
   }
}
