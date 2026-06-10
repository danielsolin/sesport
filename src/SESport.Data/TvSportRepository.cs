using Npgsql;
using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Data;

public sealed class TvSportRepository(NpgsqlDataSource dataSource)
{
   private const string TimeZoneId = "Europe/Stockholm";

   public async Task<IReadOnlyList<TvSportBroadcastListItem>> GetByDateAsync(
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
            id,
            channel_id,
            channel_name,
            title,
            description,
            categories,
            is_replay,
            original_air_date,
            starts_at,
            ends_at,
            hidden_at
         from tv_sport_broadcasts
         where starts_at >= @start and starts_at < @end
            {{hiddenFilterSql}}
            and (@hide_replays = false or is_replay = false)
            and (@category_count = 0 or categories && @categories)
         order by starts_at, channel_name nulls last, channel_id, title
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
      var broadcasts = new List<TvSportBroadcastListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         var channelId = reader.GetString(1);
         var channelName = ReadString(reader, 2) ?? channelId;
         var startsAt = reader.GetFieldValue<DateTimeOffset>(8);
         var endsAt = reader.GetFieldValue<DateTimeOffset>(9);

         broadcasts.Add(
            new TvSportBroadcastListItem(
               reader.GetGuid(0),
               FormatTime(startsAt, endsAt),
               channelName,
               reader.GetString(3),
               ReadString(reader, 4),
               string.Join(", ", reader.GetFieldValue<string[]>(5)),
               reader.GetBoolean(6),
               reader.IsDBNull(7) ? null : reader.GetFieldValue<DateOnly>(7),
               reader.IsDBNull(10) == false
            )
         );
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
         from tv_sport_broadcasts
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

   public async Task<IReadOnlyList<TvSportBroadcastActivitySource>>
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
            channel_id,
            channel_name,
            title,
            description,
            categories,
            starts_at,
            ends_at
         from tv_sport_broadcasts
         where id = any(@ids)
         order by starts_at, channel_name nulls last, channel_id, title
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("ids", ids.ToArray());

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var broadcasts = new List<TvSportBroadcastActivitySource>();

      while(await reader.ReadAsync(cancellationToken))
      {
         var channelId = reader.GetString(1);
         var channelName = ReadString(reader, 2) ?? channelId;

         broadcasts.Add(
            new TvSportBroadcastActivitySource(
               reader.GetGuid(0),
               channelName,
               reader.GetString(3),
               ReadString(reader, 4),
               reader.GetFieldValue<string[]>(5),
               reader.GetFieldValue<DateTimeOffset>(6),
               reader.GetFieldValue<DateTimeOffset>(7)
            )
         );
      }

      return broadcasts;
   }

   public async Task HideAsync(Guid id, CancellationToken cancellationToken)
   {
      const string sql = """
         update tv_sport_broadcasts
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
         update tv_sport_broadcasts
         set hidden_at = null,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);

      await command.ExecuteNonQueryAsync(cancellationToken);
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
         update tv_sport_broadcasts
         set hidden_at = coalesce(hidden_at, now()),
            updated_at = now()
         where id = any(@ids)
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("ids", ids.ToArray());

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public static DateTimeOffset ToLocal(DateTimeOffset value)
   {
      return TimeZoneHelper.ToLocal(value, TimeZoneId);
   }

   private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
   {
      return TimeZoneHelper.ToUtc(date, time, TimeZoneId);
   }

   private static string FormatTime(
      DateTimeOffset startsAt,
      DateTimeOffset endsAt
   )
   {
      var localStart = TimeZoneHelper.ToLocal(startsAt, TimeZoneId);
      var localEnd = TimeZoneHelper.ToLocal(endsAt, TimeZoneId);

      return $"{localStart:yyyy-MM-dd HH:mm}-{localEnd:HH:mm}";
   }

   private static string? ReadString(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }
}
