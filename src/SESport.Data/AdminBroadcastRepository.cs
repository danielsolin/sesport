using System.Globalization;
using System.Text;

using Npgsql;

using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Data;

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
            source_group.id as activity_group_id,
            source_group.title as activity_group_title,
            broadcasts.activity_group_draft_title,
            broadcasts.activity_group_source_kind_id,
            broadcasts.activity_group_source_activity_id
         from broadcasts
         left join entities org on org.id = broadcasts.entity_id
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
            source_group.id as activity_group_id,
            source_group.title as activity_group_title,
            broadcasts.activity_group_draft_title,
            broadcasts.activity_group_source_kind_id,
            broadcasts.activity_group_source_activity_id
         from broadcasts
         left join entities org on org.id = broadcasts.entity_id
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
            id,
            entity_id,
            channel_id,
            channel_name,
            title,
            description,
            categories,
            starts_at,
            ends_at,
            activity_group_source_kind_id,
            activity_group_source_activity_id,
            activity_group_draft_title
         from broadcasts
         where id = any(@ids)
         order by starts_at, channel_name nulls last, channel_id, title
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
               ReadString(reader, 11)
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
         reader.IsDBNull(13) ? null : reader.GetGuid(13),
         reader.IsDBNull(14) ? null : reader.GetString(14),
         reader.IsDBNull(15) ? null : reader.GetString(15),
         reader.IsDBNull(16) ? null : reader.GetString(16),
         reader.IsDBNull(17) ? null : reader.GetGuid(17)
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
            source_group.id,
            b.activity_group_source_kind_id
         from broadcasts b
         left join activities source_activity
            on source_activity.id = b.activity_group_source_activity_id
         left join activity_groups source_group
            on source_group.id = source_activity.activity_group_id
         where b.id = @id
         """;

      Guid? activityGroupId = null;
      string? sourceKindId = null;

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

         activityGroupId = reader.IsDBNull(0) ? null : reader.GetGuid(0);
         sourceKindId = ReadString(reader, 1);
      }

      if(!string.Equals(
         sourceKindId,
         BroadcastActivitySourceKindIds.ActivityGroupForActivity,
         StringComparison.Ordinal
      ))
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

      if(activityGroupId is not null)
      {
         const string groupSql = """
            update activity_groups
            set title = @title,
               updated_at = now()
            where id = @activity_group_id
            """;

         await using var groupCommand = new NpgsqlCommand(
            groupSql,
            connection,
            transaction
         );
         groupCommand.Parameters.AddWithValue(
            "activity_group_id",
            activityGroupId.Value
         );
         groupCommand.Parameters.AddWithValue("title", normalizedTitle);

         var updated = await groupCommand.ExecuteNonQueryAsync(
            cancellationToken
         );

         await transaction.CommitAsync(cancellationToken);
         return updated > 0;
      }

      const string broadcastSql = """
         update broadcasts
         set activity_group_draft_title = @title,
            updated_at = now()
         where id = @id
         """;

      await using var broadcastCommand = new NpgsqlCommand(
         broadcastSql,
         connection,
         transaction
      );
      broadcastCommand.Parameters.AddWithValue("id", broadcastId);
      broadcastCommand.Parameters.AddWithValue("title", normalizedTitle);

      var broadcastUpdated = await broadcastCommand.ExecuteNonQueryAsync(
         cancellationToken
      );

      await transaction.CommitAsync(cancellationToken);
      return broadcastUpdated > 0;
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
            id,
            entity_id,
            channel_id,
            channel_name,
            title,
            description,
            categories,
            starts_at,
            ends_at,
            activity_group_source_kind_id,
            activity_group_source_activity_id
         from broadcasts
         where id = @id
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
         reader.IsDBNull(10) ? null : reader.GetGuid(10)
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
      var categorySportId = BroadcastCategorySportIdResolver.ResolveSportId(
         broadcast.Categories
      );
      var sportId = string.IsNullOrWhiteSpace(categorySportId)
         ? await GetOrganizationSportIdAsync(
            connection,
            transaction,
            organizationEntityId,
            cancellationToken
         )
         : categorySportId;

      if(string.IsNullOrWhiteSpace(sportId) ||
         string.IsNullOrWhiteSpace(broadcast.Title))
      {
         return null;
      }

      var broadcastDate = TimeZoneHelper.ToLocal(
         broadcast.StartsAt,
         SportDay.TimeZoneId
      ).Date;
      var startDate = DateOnly.FromDateTime(broadcastDate).AddDays(-14);
      var endDate = DateOnly.FromDateTime(
         TimeZoneHelper.ToLocal(
            broadcast.EndsAt,
            SportDay.TimeZoneId
         ).Date
      ).AddDays(14);
      var normalizedBroadcastTitle = NormalizeMatchText(broadcast.Title);

      var sql = $$"""
         select
            a.id,
            a.title,
            a.activity_date
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
         var candidateScore = GetMatchScore(
            normalizedBroadcastTitle,
            NormalizeMatchText(candidateTitle),
            broadcastDate,
            candidateDate
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

   private static int GetMatchScore(
      string broadcastTitle,
      string activityTitle,
      DateTime broadcastDate,
      DateOnly activityDate
   )
   {
      if(string.IsNullOrWhiteSpace(broadcastTitle) ||
         string.IsNullOrWhiteSpace(activityTitle))
      {
         return 0;
      }

      var titleScore = 0;

      if(string.Equals(
         broadcastTitle,
         activityTitle,
         StringComparison.Ordinal
      ))
      {
         titleScore = 100;
      }
      else
      {
         var broadcastTokens = GetMatchTokens(broadcastTitle);
         var activityTokens = GetMatchTokens(activityTitle);

         if(broadcastTokens.Count < 2 || activityTokens.Count < 2)
         {
            return 0;
         }

         if(broadcastTitle.Contains(
            activityTitle,
            StringComparison.Ordinal
         ) || activityTitle.Contains(
            broadcastTitle,
            StringComparison.Ordinal
         ))
         {
            titleScore = 80;
         }
         else
         {
            var overlap = broadcastTokens.Intersect(activityTokens).Count();

            if(overlap < 2)
            {
               return 0;
            }

            titleScore = 40 + overlap * 5;
         }
      }

      var dayDistance = Math.Abs(
         DateOnly.FromDateTime(broadcastDate).DayNumber - activityDate.DayNumber
      );
      var dateScore = Math.Max(0, 14 - dayDistance);

      return titleScore * 100 + dateScore;
   }

   private static IReadOnlyCollection<string> GetMatchTokens(string value)
   {
      var normalized = NormalizeMatchText(value);

      if(string.IsNullOrWhiteSpace(normalized))
      {
         return [];
      }

      return normalized
         .Split(' ', StringSplitOptions.RemoveEmptyEntries)
         .Where(token => !IsYearToken(token))
         .Distinct(StringComparer.Ordinal)
         .ToArray();
   }

   private static bool IsYearToken(string token)
   {
      if(token.Length != 4 ||
         !int.TryParse(
            token,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var year
         ))
      {
         return false;
      }

      return year is >= 1900 and <= 2100;
   }

   private static string NormalizeMatchText(string value)
   {
      var normalized = value.Normalize(NormalizationForm.FormD);
      var builder = new StringBuilder();
      var lastWasSeparator = false;

      foreach(var character in normalized)
      {
         var category = CharUnicodeInfo.GetUnicodeCategory(character);

         if(category == UnicodeCategory.NonSpacingMark)
         {
            continue;
         }

         if(char.IsLetterOrDigit(character))
         {
            builder.Append(char.ToLowerInvariant(character));
            lastWasSeparator = false;
            continue;
         }

         if(!lastWasSeparator)
         {
            builder.Append(' ');
            lastWasSeparator = true;
         }
      }

      return builder
         .ToString()
         .Normalize(NormalizationForm.FormC)
         .Trim();
   }
}
