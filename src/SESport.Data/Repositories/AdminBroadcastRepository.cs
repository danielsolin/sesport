using Npgsql;

using SESport.Core.Broadcast;
using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class AdminBroadcastRepository(NpgsqlDataSource dataSource)
{
   public async Task<BroadcastListItem?> GetByIdAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            broadcasts.id,
            broadcasts.entity_id,
            broadcasts.channel_id,
            broadcasts.channel_name,
            broadcasts.title,
            broadcasts.description,
            broadcasts.categories,
            broadcasts.is_replay,
            broadcasts.original_air_date,
            broadcasts.starts_at,
            broadcasts.ends_at,
            broadcasts.hidden_at,
            org.canonical_name as organization_name,
            organization_sport.name as organization_sport_name,
            source_group.id as activity_group_id,
            source_group.title as activity_group_title,
            broadcasts.activity_group_draft_title,
            broadcasts.activity_group_source_kind_id,
            broadcasts.activity_group_source_activity_id,
            broadcasts.source_key
         from broadcasts
         left join entities org on org.id = broadcasts.entity_id
         left join sports organization_sport
            on organization_sport.id = org.sport_id
         left join activities source_activity
            on source_activity.id = broadcasts.activity_group_source_activity_id
         left join activity_groups source_group
            on source_group.id = source_activity.activity_group_id
         where broadcasts.id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return ReadBroadcastListItem(reader);
   }

   public async Task<IReadOnlyList<BroadcastListItem>> GetByDateAsync(
      DateOnly date,
      bool hideReplays,
      bool showHidden,
      IReadOnlyCollection<string> categories,
      CancellationToken cancellationToken
   )
   {
      var window = SportDay.ForDate(date);
      var start = ToUtc(window.StartDate, window.Cutoff);
      var end = ToUtc(window.EndDateExclusive, window.Cutoff);

      var hiddenFilterSql = showHidden
         ? ""
         : "and hidden_at is null";

      var sql = $$"""
         select
            broadcasts.id,
            broadcasts.entity_id,
            broadcasts.channel_id,
            broadcasts.channel_name,
            broadcasts.title,
            broadcasts.description,
            broadcasts.categories,
            broadcasts.is_replay,
            broadcasts.original_air_date,
            broadcasts.starts_at,
            broadcasts.ends_at,
            broadcasts.hidden_at,
            org.canonical_name as organization_name,
            organization_sport.name as organization_sport_name,
            source_group.id as activity_group_id,
            source_group.title as activity_group_title,
            broadcasts.activity_group_draft_title,
            broadcasts.activity_group_source_kind_id,
            broadcasts.activity_group_source_activity_id,
            broadcasts.source_key
         from broadcasts
         left join entities org on org.id = broadcasts.entity_id
         left join sports organization_sport
            on organization_sport.id = org.sport_id
         left join activities source_activity
            on source_activity.id = broadcasts.activity_group_source_activity_id
         left join activity_groups source_group
            on source_group.id = source_activity.activity_group_id
         where broadcasts.starts_at >= @start
            and broadcasts.starts_at < @end
            {{hiddenFilterSql}}
            and (@hide_replays = false or broadcasts.is_replay = false)
            and (@category_count = 0 or broadcasts.categories && @categories)
         order by
            broadcasts.starts_at,
            broadcasts.channel_name nulls last,
            broadcasts.channel_id,
            broadcasts.title
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("start", start);
      command.Parameters.AddWithValue("end", end);
      command.Parameters.AddWithValue("hide_replays", hideReplays);
      command.Parameters.AddWithValue("category_count", categories.Count);
      command.Parameters.AddWithValue("categories", categories.ToArray());

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var broadcasts = new List<BroadcastListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         broadcasts.Add(ReadBroadcastListItem(reader));
      }

      return broadcasts;
   }

   public async Task<IReadOnlyCollection<string>> GetCategoriesForDateAsync(
      DateOnly date,
      bool hideReplays,
      bool showHidden,
      CancellationToken cancellationToken
   )
   {
      var window = SportDay.ForDate(date);
      var start = ToUtc(window.StartDate, window.Cutoff);
      var end = ToUtc(window.EndDateExclusive, window.Cutoff);

      var hiddenFilterSql = showHidden
         ? ""
         : "and hidden_at is null";

      string sql = $$"""
         select distinct unnest(categories) as category
         from broadcasts
         where starts_at >= @start and starts_at < @end
            {{hiddenFilterSql}}
            and (@hide_replays = false or is_replay = false)
         order by category
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("start", start);
      command.Parameters.AddWithValue("end", end);
      command.Parameters.AddWithValue("hide_replays", hideReplays);

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var categories = new List<string>();

      while(await reader.ReadAsync(cancellationToken))
      {
         categories.Add(reader.GetString(0));
      }

      return categories;
   }

   public async Task<IReadOnlyList<BroadcastActivitySource>>
      GetActivitySourcesAsync(
         IReadOnlyCollection<Guid> ids,
         CancellationToken cancellationToken
      )
   {
      if(ids.Count == 0)
      {
         return [];
      }

      const string sql = """
         select
            b.id,
            b.entity_id,
            b.channel_id,
            b.channel_name,
            b.title,
            b.description,
            b.categories,
            b.starts_at,
            b.ends_at,
            b.activity_group_source_kind_id,
            b.activity_group_source_activity_id,
            b.activity_group_draft_title,
            e.sport_id,
            e.canonical_name
         from broadcasts b
         left join entities e on e.id = b.entity_id
         where b.id = any(@ids)
         order by b.starts_at, b.channel_name nulls last, b.channel_id, b.title
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("ids", ids.ToArray());

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var broadcasts = new List<BroadcastActivitySource>();

      while(await reader.ReadAsync(cancellationToken))
      {
         var channelId = reader.GetString(2);
         var channelName = ReadString(reader, 3) ?? channelId;

         broadcasts.Add(
            new BroadcastActivitySource(
               reader.GetGuid(0),
               channelName,
               reader.GetString(4),
               ReadString(reader, 5),
               reader.GetFieldValue<string[]>(6),
               reader.GetFieldValue<DateTimeOffset>(7),
               reader.GetFieldValue<DateTimeOffset>(8),
               reader.IsDBNull(1) ? null : reader.GetGuid(1),
               ReadString(reader, 9),
               reader.IsDBNull(10) ? null : reader.GetGuid(10),
               ReadString(reader, 11),
               ReadString(reader, 12),
               OrganizationName: ReadString(reader, 13)
            )
         );
      }

      return broadcasts;
   }

   public async Task HideAsync(Guid id, CancellationToken cancellationToken)
   {
      const string sql = """
         update broadcasts
         set hidden_at = coalesce(hidden_at, now()),
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task ShowAsync(Guid id, CancellationToken cancellationToken)
   {
      const string sql = """
         update broadcasts
         set hidden_at = null,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task UpdateTitleAsync(
      Guid id,
      string title,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update broadcasts
         set title = @title,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("title", title);

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task UpdateDescriptionAsync(
      Guid id,
      string? description,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update broadcasts
         set description = @description,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue(
         "description",
         (object?)description ?? DBNull.Value
      );

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task UpdateCategoriesAsync(
      Guid id,
      IReadOnlyCollection<string> categories,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update broadcasts
         set categories = @categories,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("categories", categories.ToArray());

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task UpdateOrganizationAsync(
      Guid id,
      Guid? organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      string? activitySourceKindId = null;
      Guid? activitySourceActivityId = null;

      if(organizationEntityId is not null)
      {
         activitySourceKindId =
            BroadcastActivitySourceKindIds.ActivityGroupForActivity;

         var broadcast = await LoadBroadcastActivitySourceAsync(
            connection,
            transaction,
            id,
            cancellationToken
         );

         if(broadcast is not null)
         {
            activitySourceActivityId =
               await FindMatchingActivityIdAsync(
                  connection,
                  transaction,
                  organizationEntityId.Value,
                  broadcast,
                  cancellationToken
               );
         }
      }

      const string sql = """
         update broadcasts
         set entity_id = @entity_id,
            activity_group_source_kind_id = @activity_group_source_kind_id,
            activity_group_source_activity_id =
               @activity_group_source_activity_id,
            updated_at = now()
         where id = @id
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue(
         "entity_id",
         (object?)organizationEntityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "activity_group_source_kind_id",
         (object?)activitySourceKindId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "activity_group_source_activity_id",
         (object?)activitySourceActivityId ?? DBNull.Value
      );

      await command.ExecuteNonQueryAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
   }

   public async Task HideAsync(
      IReadOnlyCollection<Guid> ids,
      CancellationToken cancellationToken
   )
   {
      if(ids.Count == 0)
      {
         return;
      }

      const string sql = """
         update broadcasts
         set hidden_at = coalesce(hidden_at, now()),
            updated_at = now()
         where id = any(@ids)
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("ids", ids.ToArray());

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
   {
      return TimeZoneHelper.ToUtc(date, time, SportDay.TimeZoneId);
   }

   private static string? ReadString(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static BroadcastListItem ReadBroadcastListItem(
      NpgsqlDataReader reader
   )
   {
      var channelId = reader.GetString(2);
      var channelName = ReadString(reader, 3) ?? channelId;
      var startsAt = reader.GetFieldValue<DateTimeOffset>(9);
      var endsAt = reader.GetFieldValue<DateTimeOffset>(10);

      return new BroadcastListItem(
         reader.GetGuid(0),
         BroadcastListDisplayFormatter.FormatTimeText(
            startsAt,
            endsAt
         ),
         channelName,
         reader.GetString(4),
         ReadString(reader, 5),
         reader.GetFieldValue<string[]>(6),
         reader.GetBoolean(7),
         reader.IsDBNull(8) ? null : reader.GetFieldValue<DateOnly>(8),
         reader.IsDBNull(11) == false,
         reader.IsDBNull(1) ? null : reader.GetGuid(1),
         reader.IsDBNull(12) ? null : reader.GetString(12),
         reader.IsDBNull(13) ? null : reader.GetString(13),
         reader.IsDBNull(14) ? null : reader.GetGuid(14),
         reader.IsDBNull(15) ? null : reader.GetString(15),
         reader.IsDBNull(16) ? null : reader.GetString(16),
         reader.IsDBNull(17) ? null : reader.GetString(17),
         reader.IsDBNull(18) ? null : reader.GetGuid(18),
         reader.GetString(19)
      );
   }

   public async Task<bool> UpdateActivityGroupTitleAsync(
      Guid broadcastId,
      string title,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      const string loadSql = """
         select
            b.activity_group_source_kind_id,
            b.entity_id
         from broadcasts b
         where b.id = @id
         """;

      string? sourceKindId = null;
      Guid? organizationEntityId = null;

      await using(
         var command = new NpgsqlCommand(loadSql, connection, transaction)
      )
      {
         command.Parameters.AddWithValue("id", broadcastId);

         await using var reader = await command.ExecuteReaderAsync(
            cancellationToken
         );

         if(!await reader.ReadAsync(cancellationToken))
         {
            return false;
         }

         sourceKindId = ReadString(reader, 0);
         organizationEntityId = reader.IsDBNull(1)
            ? null
            : reader.GetGuid(1);
      }

      if(!string.Equals(
         sourceKindId,
         BroadcastActivitySourceKindIds.ActivityGroupForActivity,
         StringComparison.Ordinal
      ) && organizationEntityId is null)
      {
         return false;
      }

      var normalizedTitle = title.Trim();

      if(string.IsNullOrWhiteSpace(normalizedTitle))
      {
         const string clearSql = """
            update broadcasts
            set activity_group_source_kind_id = null,
               activity_group_source_activity_id = null,
               activity_group_draft_title = null,
               updated_at = now()
            where id = @id
            """;

         await using var clearCommand = new NpgsqlCommand(
            clearSql,
            connection,
            transaction
         );
         clearCommand.Parameters.AddWithValue("id", broadcastId);

         var cleared = await clearCommand.ExecuteNonQueryAsync(
            cancellationToken
         );

         await transaction.CommitAsync(cancellationToken);
         return cleared > 0;
      }

      const string broadcastSql = """
         update broadcasts
         set activity_group_source_kind_id = @source_kind_id,
            activity_group_source_activity_id = null,
            activity_group_draft_title = @title,
            updated_at = now()
         where id = @id
         """;

      await using var broadcastCommand = new NpgsqlCommand(
         broadcastSql,
         connection,
         transaction
      );
      broadcastCommand.Parameters.AddWithValue("id", broadcastId);
      broadcastCommand.Parameters.AddWithValue(
         "source_kind_id",
         BroadcastActivitySourceKindIds.ActivityGroupForActivity
      );
      broadcastCommand.Parameters.AddWithValue("title", normalizedTitle);

      var broadcastUpdated = await broadcastCommand.ExecuteNonQueryAsync(
         cancellationToken
      );

      await transaction.CommitAsync(cancellationToken);
      return broadcastUpdated > 0;
   }

   public async Task<bool> UpdateActivityGroupAsync(
      Guid broadcastId,
      Guid activityGroupId,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      const string sourceSql = """
         select
            b.activity_group_source_kind_id,
            b.entity_id
         from broadcasts b
         where b.id = @id
         """;

      string? sourceKindId;
      Guid? organizationEntityId;

      await using(
         var sourceCommand = new NpgsqlCommand(
            sourceSql,
            connection,
            transaction
         )
      )
      {
         sourceCommand.Parameters.AddWithValue("id", broadcastId);

         await using var reader = await sourceCommand.ExecuteReaderAsync(
            cancellationToken
         );

         if(!await reader.ReadAsync(cancellationToken))
         {
            return false;
         }

         sourceKindId = ReadString(reader, 0);
         organizationEntityId = reader.IsDBNull(1)
            ? null
            : reader.GetGuid(1);
      }

      if(organizationEntityId is null ||
         (!string.IsNullOrWhiteSpace(sourceKindId) &&
            !string.Equals(
               sourceKindId,
               BroadcastActivitySourceKindIds.ActivityGroupForActivity,
               StringComparison.Ordinal
            )))
      {
         return false;
      }

      const string activitySql = """
         select a.id
         from activities a
         join broadcasts b on b.id = @broadcast_id
         where a.activity_group_id = @activity_group_id
            and exists (
               select 1
               from activity_entity_links al
               where al.activity_id = a.id
                  and al.organization_entity_id = @organization_entity_id
            )
         order by
            abs(extract(epoch from (a.starts_at - b.starts_at))),
            a.activity_date,
            a.id
         limit 1
         """;

      Guid? sourceActivityId;
      await using(
         var activityCommand = new NpgsqlCommand(
            activitySql,
            connection,
            transaction
         )
      )
      {
         activityCommand.Parameters.AddWithValue(
            "broadcast_id",
            broadcastId
         );
         activityCommand.Parameters.AddWithValue(
            "activity_group_id",
            activityGroupId
         );
         activityCommand.Parameters.AddWithValue(
            "organization_entity_id",
            organizationEntityId.Value
         );

         sourceActivityId = (Guid?)await activityCommand.ExecuteScalarAsync(
            cancellationToken
         );
      }

      if(sourceActivityId is null)
      {
         return false;
      }

      const string updateSql = """
         update broadcasts
         set activity_group_source_kind_id = @source_kind_id,
            activity_group_source_activity_id = @source_activity_id,
            activity_group_draft_title = null,
            updated_at = now()
         where id = @id
         """;

      await using var updateCommand = new NpgsqlCommand(
         updateSql,
         connection,
         transaction
      );
      updateCommand.Parameters.AddWithValue(
         "source_kind_id",
         BroadcastActivitySourceKindIds.ActivityGroupForActivity
      );
      updateCommand.Parameters.AddWithValue(
         "source_activity_id",
         sourceActivityId.Value
      );
      updateCommand.Parameters.AddWithValue("id", broadcastId);

      var updated = await updateCommand.ExecuteNonQueryAsync(
         cancellationToken
      );

      await transaction.CommitAsync(cancellationToken);
      return updated > 0;
   }

   private async Task<BroadcastActivitySource?>
      LoadBroadcastActivitySourceAsync(
         NpgsqlConnection connection,
         NpgsqlTransaction transaction,
         Guid id,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            b.id,
            b.entity_id,
            b.channel_id,
            b.channel_name,
            b.title,
            b.description,
            b.categories,
            b.starts_at,
            b.ends_at,
            b.activity_group_source_kind_id,
            b.activity_group_source_activity_id,
            e.sport_id,
            e.canonical_name
         from broadcasts b
         left join entities e on e.id = b.entity_id
         where b.id = @id
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      var channelId = reader.GetString(2);
      var channelName = ReadString(reader, 3) ?? channelId;

      return new BroadcastActivitySource(
         reader.GetGuid(0),
         channelName,
         reader.GetString(4),
         ReadString(reader, 5),
         reader.GetFieldValue<string[]>(6),
         reader.GetFieldValue<DateTimeOffset>(7),
         reader.GetFieldValue<DateTimeOffset>(8),
         reader.IsDBNull(1) ? null : reader.GetGuid(1),
         ReadString(reader, 9),
         reader.IsDBNull(10) ? null : reader.GetGuid(10),
         EntitySportId: ReadString(reader, 11),
         OrganizationName: ReadString(reader, 12)
      );
   }

   private async Task<Guid?> FindMatchingActivityIdAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid organizationEntityId,
      BroadcastActivitySource broadcast,
      CancellationToken cancellationToken
   )
   {
      var sportId = await GetOrganizationSportIdAsync(
         connection,
         transaction,
         organizationEntityId,
         cancellationToken
      );

      if(string.IsNullOrWhiteSpace(sportId) ||
         string.IsNullOrWhiteSpace(broadcast.Title))
      {
         return null;
      }

      var broadcastLocalStart = TimeZoneHelper.ToLocal(
         broadcast.StartsAt,
         SportDay.TimeZoneId
      ).DateTime;
      var broadcastDate = DateOnly.FromDateTime(broadcastLocalStart);
      var startDate = broadcastDate.AddDays(
         -ActivityGroupDefaults.CandidateDateRangeDays
      );
      var endDate = broadcastDate.AddDays(
         ActivityGroupDefaults.CandidateDateRangeDays
      );
      var sql = $$"""
         select
            a.id,
            a.title,
            a.activity_date,
            a.local_start_time
         from activities a
         where a.sport_id = @sport_id
            and a.activity_group_id is not null
            and a.activity_date between @start_date and @end_date
            and exists (
               select 1
               from activity_entity_links al
               where al.activity_id = a.id
                  and al.organization_entity_id = @organization_entity_id
            )
         order by a.activity_date, a.local_start_time nulls last, a.title
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("sport_id", sportId);
      command.Parameters.AddWithValue("start_date", startDate);
      command.Parameters.AddWithValue("end_date", endDate);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var bestCandidateId = (Guid?)null;
      var bestCandidateScore = 0;

      while(await reader.ReadAsync(cancellationToken))
      {
         var candidateId = reader.GetGuid(0);
         var candidateTitle = reader.GetString(1);
         var candidateDate = reader.GetFieldValue<DateOnly>(2);
         var candidateTime = reader.IsDBNull(3)
            ? TimeOnly.FromDateTime(broadcastLocalStart)
            : reader.GetFieldValue<TimeOnly>(3);
         var candidateLocalStart = candidateDate.ToDateTime(candidateTime);
         var candidateScore = BroadcastActivityMatchScorer.GetScore(
            broadcast.Title,
            candidateTitle,
            broadcastLocalStart,
            candidateLocalStart
         );

         if(candidateScore <= bestCandidateScore)
         {
            continue;
         }

         bestCandidateScore = candidateScore;
         bestCandidateId = candidateId;
      }

      return bestCandidateScore > 0 ? bestCandidateId : null;
   }

   private static async Task<string?> GetOrganizationSportIdAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select sport_id
         from entities
         where id = @id
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", organizationEntityId);

      return (string?)await command.ExecuteScalarAsync(cancellationToken);
   }

}
