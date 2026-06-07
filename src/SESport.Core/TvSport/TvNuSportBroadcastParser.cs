using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SESport.Core.TvSport;

public sealed class TvNuSportBroadcastParser
{
   private const string DefaultSourceKey = "tv-nu-sport";
   private const string TimeZoneId = "Europe/Stockholm";

   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   );

   public async Task<IReadOnlyCollection<TvSportBroadcast>> ParseAsync(
      Stream stream,
      CancellationToken cancellationToken
   )
   {
      using var reader = new StreamReader(
         stream,
         Encoding.UTF8,
         detectEncodingFromByteOrderMarks: true,
         bufferSize: 4096,
         leaveOpen: true
      );

      var html = await reader.ReadToEndAsync(cancellationToken);
      var initialStateJson = ExtractInitialStateJson(html);

      if(initialStateJson is null)
      {
         return [];
      }

      var state = JsonSerializer.Deserialize<TvNuSportState>(
         initialStateJson,
         JsonOptions
      );

      if(state?.SportPageSchedule is null)
      {
         return [];
      }

      var broadcasts = new List<TvSportBroadcast>();

      foreach(var scheduleItem in state.SportPageSchedule)
      {
         cancellationToken.ThrowIfCancellationRequested();

         broadcasts.AddRange(CreateBroadcasts(scheduleItem));
      }

      return broadcasts;
   }

   private static IEnumerable<TvSportBroadcast> CreateBroadcasts(
      TvNuSportScheduleItem scheduleItem
   )
   {
      if(string.IsNullOrWhiteSpace(scheduleItem.Title))
      {
         yield break;
      }

      var categories = GetCategories(scheduleItem);

      foreach(var broadcast in scheduleItem.Broadcasts)
      {
         if(!TryCreateBroadcast(
            scheduleItem,
            categories,
            "broadcast",
            broadcast.Channel?.Slug,
            broadcast.Channel?.Name,
            broadcast.StartTime,
            broadcast.EndTime,
            broadcast.IsRerun,
            out var tvSportBroadcast
         ))
         {
            continue;
         }

         yield return tvSportBroadcast;
      }

      foreach(var playEpisode in scheduleItem.PlayEpisodes)
      {
         if(!TryCreateBroadcast(
            scheduleItem,
            categories,
            "stream",
            playEpisode.PlayProvider?.Slug,
            playEpisode.PlayProvider?.Name,
            playEpisode.StreamStart,
            playEpisode.StreamEnd,
            isReplay: false,
            out var tvSportBroadcast
         ))
         {
            continue;
         }

         yield return tvSportBroadcast;
      }
   }

   private static bool TryCreateBroadcast(
      TvNuSportScheduleItem scheduleItem,
      IReadOnlyCollection<string> categories,
      string itemKind,
      string? channelId,
      string? channelName,
      long? startsAtValue,
      long? endsAtValue,
      bool isReplay,
      out TvSportBroadcast broadcast
   )
   {
      broadcast = null!;

      if(
         string.IsNullOrWhiteSpace(channelId) ||
         !startsAtValue.HasValue ||
         !endsAtValue.HasValue
      )
      {
         return false;
      }

      var startsAt = DateTimeOffset.FromUnixTimeMilliseconds(
         startsAtValue.Value
      );
      var endsAt = DateTimeOffset.FromUnixTimeMilliseconds(
         endsAtValue.Value
      );

      if(endsAt <= startsAt)
      {
         return false;
      }

      var normalizedChannelId = channelId.Trim();
      var title = NormalizeOptionalText(scheduleItem.Title);

      if(string.IsNullOrWhiteSpace(title))
      {
         return false;
      }

      var externalId = CreateExternalId(
         scheduleItem.Id,
         itemKind,
         normalizedChannelId,
         startsAt
      );
      var fingerprint = CreateFingerprint(
         DefaultSourceKey,
         itemKind,
         normalizedChannelId,
         startsAt,
         title
      );

      broadcast = new TvSportBroadcast(
         DeterministicGuid.Create($"tv-sport-broadcast:{fingerprint}"),
         DefaultSourceKey,
         externalId,
         fingerprint,
         normalizedChannelId,
         NormalizeOptionalText(channelName),
         title,
         NormalizeOptionalText(scheduleItem.Description),
         categories,
         isReplay,
         null,
         startsAt,
         endsAt,
         TimeZoneId,
         null
      );

      return true;
   }

   private static IReadOnlyCollection<string> GetCategories(
      TvNuSportScheduleItem scheduleItem
   )
   {
      return new[]
      {
         scheduleItem.SportGroup,
         scheduleItem.Sport,
         scheduleItem.Subtitle,
         scheduleItem.Tournament
      }
      .Select(NormalizeOptionalText)
      .Where(value => !string.IsNullOrWhiteSpace(value))
      .Select(value => value!)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();
   }

   private static string CreateExternalId(
      string scheduleId,
      string itemKind,
      string channelId,
      DateTimeOffset startsAt
   )
   {
      return string.Join(
         ":",
         scheduleId,
         itemKind,
         NormalizeKey(channelId),
         startsAt.ToUniversalTime().ToString(
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture
         )
      );
   }

   private static string CreateFingerprint(
      string sourceKey,
      string itemKind,
      string channelId,
      DateTimeOffset startsAt,
      string title
   )
   {
      var value = string.Join(
         "|",
         sourceKey,
         itemKind,
         NormalizeKey(channelId),
         startsAt.ToUniversalTime().ToString(
            "yyyy-MM-ddTHH:mm:ssZ",
            CultureInfo.InvariantCulture
         ),
         NormalizeKey(title)
      );
      var hash = System.Security.Cryptography.SHA256.HashData(
         Encoding.UTF8.GetBytes(value)
      );

      return Convert.ToHexString(hash)[..32].ToLowerInvariant();
   }

   private static string NormalizeKey(string value)
   {
      return NormalizeText(value).ToUpperInvariant();
   }

   private static string NormalizeText(string value)
   {
      return value.Replace("\\u0026", "&").Trim();
   }

   private static string? NormalizeOptionalText(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      return NormalizeText(value);
   }

   private static string? ExtractInitialStateJson(string html)
   {
      const string marker = "__INITIAL_STATE__ = ";
      var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);

      if(markerIndex < 0)
      {
         return null;
      }

      var startIndex = markerIndex + marker.Length;

      while(
         startIndex < html.Length &&
         char.IsWhiteSpace(html[startIndex])
      )
      {
         startIndex++;
      }

      if(startIndex >= html.Length || html[startIndex] != '"')
      {
         return null;
      }

      var endIndex = startIndex + 1;
      var escaped = false;

      while(endIndex < html.Length)
      {
         var current = html[endIndex];

         if(escaped)
         {
            escaped = false;
         }
         else if(current == '\\')
         {
            escaped = true;
         }
         else if(current == '"')
         {
            break;
         }

         endIndex++;
      }

      if(endIndex >= html.Length)
      {
         return null;
      }

      var literal = html[startIndex..(endIndex + 1)];

      return JsonSerializer.Deserialize<string>(literal);
   }

   private sealed class TvNuSportState
   {
      [JsonPropertyName("sportPageSchedule")]
      public List<TvNuSportScheduleItem> SportPageSchedule { get; set; } = [];
   }

   private sealed class TvNuSportScheduleItem
   {
      [JsonPropertyName("id")]
      public string Id { get; set; } = string.Empty;

      [JsonPropertyName("title")]
      public string? Title { get; set; }

      [JsonPropertyName("description")]
      public string? Description { get; set; }

      [JsonPropertyName("sportGroup")]
      public string? SportGroup { get; set; }

      [JsonPropertyName("sport")]
      public string? Sport { get; set; }

      [JsonPropertyName("subtitle")]
      public string? Subtitle { get; set; }

      [JsonPropertyName("tournament")]
      public string? Tournament { get; set; }

      [JsonPropertyName("broadcasts")]
      public List<TvNuSportBroadcastItem> Broadcasts { get; set; } = [];

      [JsonPropertyName("playEpisodes")]
      public List<TvNuSportPlayEpisodeItem> PlayEpisodes { get; set; } = [];
   }

   private sealed class TvNuSportBroadcastItem
   {
      [JsonPropertyName("startTime")]
      public long? StartTime { get; set; }

      [JsonPropertyName("endTime")]
      public long? EndTime { get; set; }

      [JsonPropertyName("isRerun")]
      public bool IsRerun { get; set; }

      [JsonPropertyName("channel")]
      public TvNuSportChannelItem? Channel { get; set; }
   }

   private sealed class TvNuSportPlayEpisodeItem
   {
      [JsonPropertyName("streamStart")]
      public long? StreamStart { get; set; }

      [JsonPropertyName("streamEnd")]
      public long? StreamEnd { get; set; }

      [JsonPropertyName("playProvider")]
      public TvNuSportChannelItem? PlayProvider { get; set; }
   }

   private sealed class TvNuSportChannelItem
   {
      [JsonPropertyName("name")]
      public string? Name { get; set; }

      [JsonPropertyName("slug")]
      public string? Slug { get; set; }
   }
}
