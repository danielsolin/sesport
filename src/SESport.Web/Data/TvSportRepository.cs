using Npgsql;

namespace SESport.Web.Data;

public sealed class TvSportRepository(NpgsqlDataSource dataSource)
{
   private const string TimeZoneId = "Europe/Stockholm";

   public async Task<IReadOnlyList<TvSportBroadcastListItem>> GetByDateAsync(
      DateOnly date,
      bool hideReplays,
      IReadOnlyCollection<string> categories,
      CancellationToken cancellationToken
   )
   {
      var start = ToUtc(date, TimeOnly.MinValue);
      var end = ToUtc(date.AddDays(1), TimeOnly.MinValue);

      var sql = """
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
            ends_at
         from tv_sport_broadcasts
         where starts_at >= @start and starts_at < @end
            and hidden_at is null
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
               reader.IsDBNull(7) ? null : reader.GetFieldValue<DateOnly>(7)
            )
         );
      }

      return broadcasts;
   }

   public async Task<IReadOnlyCollection<string>> GetCategoriesForDateAsync(
      DateOnly date,
      bool hideReplays,
      CancellationToken cancellationToken
   )
   {
      var start = ToUtc(date, TimeOnly.MinValue);
      var end = ToUtc(date.AddDays(1), TimeOnly.MinValue);

      const string sql = """
         select distinct unnest(categories) as category
         from tv_sport_broadcasts
         where starts_at >= @start and starts_at < @end
            and hidden_at is null
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

   private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
   {
      var local = date.ToDateTime(time);
      var timeZone = ResolveTimeZone();
      var offset = timeZone.GetUtcOffset(local);

      return new DateTimeOffset(local, offset).ToUniversalTime();
   }

   private static string FormatTime(
      DateTimeOffset startsAt,
      DateTimeOffset endsAt
   )
   {
      var timeZone = ResolveTimeZone();
      var localStart = TimeZoneInfo.ConvertTime(startsAt, timeZone);
      var localEnd = TimeZoneInfo.ConvertTime(endsAt, timeZone);

      return $"{localStart:yyyy-MM-dd HH:mm}-{localEnd:HH:mm}";
   }

   private static TimeZoneInfo ResolveTimeZone()
   {
      try
      {
         return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
      }
      catch(TimeZoneNotFoundException)
      {
         if(
            TimeZoneInfo.TryConvertIanaIdToWindowsId(
               TimeZoneId,
               out var windowsId
            )
         )
         {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
         }

         return TimeZoneInfo.Utc;
      }
   }

   private static string? ReadString(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }
}
