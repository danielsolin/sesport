using SESport.Core.Broadcast;
using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Identifiers;
using SESport.Core.Sources;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SESport.Tools.BroadcastImporter;

public sealed class BroadcastParser
{
   private const string DefaultSourceKey = "tvnu";
   private static readonly string[] FingerprintChannelSuffixes =
   [
      "-se",
      "-sverige",
      "-sweden"
   ];

   private const string DisneyFingerprintChannelKey = "disney";
   private const string SportbladetFingerprintChannelKey =
      "sportbladet-plus";

   private static readonly IReadOnlyDictionary<string, string>
      FingerprintChannelAliases = new Dictionary<string, string>(
         StringComparer.OrdinalIgnoreCase
      )
      {
         ["disneyplus"] = DisneyFingerprintChannelKey,
         ["sportbladet-play"] = SportbladetFingerprintChannelKey
      };

   private readonly string sourceKey;

   public BroadcastParser()
      : this(DefaultSourceKey)
   {
   }

   public BroadcastParser(string sourceKey)
   {
      this.sourceKey = NormalizeSourceKey(sourceKey);
   }

   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   );

   private static readonly Regex RenderedRowRegex = new(
      @"<li class=""_37xCg nSLmX"">(?<content>.*?)</li>",
      RegexOptions.Singleline | RegexOptions.Compiled
   );

   private static readonly Regex RenderedTitleRegex = new(
      @"aria-label=""Link - \d{2}:\d{2}, (?<title>[^""]+)""",
      RegexOptions.Singleline | RegexOptions.Compiled
   );

   private static readonly Regex RenderedHrefRegex = new(
      @"<a href=""(?<href>[^""]+)""",
      RegexOptions.Singleline | RegexOptions.Compiled
   );

   private static readonly Regex RenderedStartRegex = new(
      @"<time datetime=""(?<datetime>[^""]+)""",
      RegexOptions.Singleline | RegexOptions.Compiled
   );

   private static readonly Regex RenderedChannelRegex = new(
      @"<span class=""Oz76s"">[^<]*</span></div>\s*(?<name>[^<]+?)\s*</div>",
      RegexOptions.Singleline | RegexOptions.Compiled
   );

   private static readonly Regex RenderedCategoryRegex = new(
      @"<div class=""_2ZygK"">\s*<div class=""_2HFK6""></div>\s*"
      + @"(?<category>[^<]+?)<span class=""ss5Ll""",
      RegexOptions.Singleline | RegexOptions.Compiled
   );

   private static readonly Regex RenderedImageRegex = new(
      @"<meta itemProp=""image"" content=""(?<image>[^""]+)""",
      RegexOptions.Singleline | RegexOptions.Compiled
   );

   public async Task<BroadcastParseResult> ParseAsync(
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
      var broadcasts = new List<Broadcast>();

      var initialStateJson = ExtractInitialStateJson(html);

      if(initialStateJson is not null)
      {
         var state = JsonSerializer.Deserialize<BroadcastState>(
            initialStateJson,
            JsonOptions
         );

         if(state?.SportPageSchedule is not null)
         {
            foreach(var scheduleItem in state.SportPageSchedule)
            {
               cancellationToken.ThrowIfCancellationRequested();
               broadcasts.AddRange(CreateBroadcasts(scheduleItem));
            }
         }
      }

      broadcasts.AddRange(ParseRenderedRows(html));

      var deduplicatedBroadcasts = DeduplicateBroadcasts(broadcasts);

      return new BroadcastParseResult(
         deduplicatedBroadcasts,
         broadcasts.Count - deduplicatedBroadcasts.Count
      );
   }

   private static IReadOnlyCollection<Broadcast> DeduplicateBroadcasts(
      IReadOnlyCollection<Broadcast> broadcasts
   )
   {
      var deduplicated = new List<Broadcast>();

      foreach(var group in broadcasts.GroupBy(item => item.Fingerprint))
      {
         var sourceBroadcastGroups = group
            .Where(item => !IsRenderedBroadcast(item))
            .GroupBy(item => item.StartsAt)
            .ToList();

         if(sourceBroadcastGroups.Count == 0)
         {
            deduplicated.Add(group.First());
            continue;
         }

         for(var index = 0; index < sourceBroadcastGroups.Count; index++)
         {
            var sourceBroadcastGroup = sourceBroadcastGroups[index];
            var broadcast = sourceBroadcastGroup.First() with
            {
               StreamLinks = MergeStreamLinks(sourceBroadcastGroup)
            };

            if(index == 0)
            {
               deduplicated.Add(broadcast);
               continue;
            }

            var fingerprint = CreateCollisionFingerprint(broadcast);
            deduplicated.Add(
               broadcast with
               {
                  Id = DeterministicGuid.Create(
                     $"broadcast:{fingerprint}"
                  ),
                  Fingerprint = fingerprint
               }
            );
         }
      }

      return deduplicated;
   }

   private static IReadOnlyList<BroadcastStreamLink> MergeStreamLinks(
      IEnumerable<Broadcast> broadcasts
   )
   {
      return broadcasts
         .SelectMany(broadcast => broadcast.StreamLinks)
         .GroupBy(
            link => link.Url,
            StringComparer.OrdinalIgnoreCase
         )
         .Select(group => group.First())
         .ToArray();
   }

   private static bool IsRenderedBroadcast(Broadcast broadcast)
   {
      return broadcast.ExternalId.Contains(
         ":rendered:",
         StringComparison.Ordinal
      );
   }

   private static string CreateCollisionFingerprint(Broadcast broadcast)
   {
      var separatorIndex = broadcast.ExternalId.LastIndexOf(':');
      var collisionIdentity = separatorIndex > 0
         ? broadcast.ExternalId[..separatorIndex]
         : broadcast.ExternalId;
      var value = string.Join(
         "|",
         broadcast.Fingerprint,
         collisionIdentity
      );
      var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));

      return Convert.ToHexString(hash)[..32].ToLowerInvariant();
   }

   private IEnumerable<Broadcast> ParseRenderedRows(string html)
   {
      var rows = new List<RenderedRow>();

      foreach(Match match in RenderedRowRegex.Matches(html))
      {
         if(!TryParseRenderedRow(match.Groups["content"].Value, out var row))
         {
            continue;
         }

         rows.Add(row);
      }

      for(var index = 0; index < rows.Count; index++)
      {
         var row = rows[index];
         var endsAt = row.EndsAt ?? FindNextStart(rows, index) ??
            row.StartsAt.AddMinutes(1);

         foreach(var channelName in row.ChannelNames)
         {
            if(!TryCreateRenderedBroadcast(
               row,
               channelName,
               endsAt,
               out var broadcast
            ))
            {
               continue;
            }

            yield return broadcast;
         }
      }
   }

   private static bool TryParseRenderedRow(
      string html,
      out RenderedRow row
   )
   {
      row = null!;

      var title = ExtractRenderedValue(RenderedTitleRegex, html, "title");
      var href = ExtractRenderedValue(RenderedHrefRegex, html, "href");
      var startText = ExtractRenderedValue(
         RenderedStartRegex,
         html,
         "datetime"
      );

      if(
         string.IsNullOrWhiteSpace(title) ||
         string.IsNullOrWhiteSpace(href) ||
         string.IsNullOrWhiteSpace(startText) ||
         !TryParseStockholmDateTime(startText, out var startsAt)
      )
      {
         return false;
      }

      var channelNames = RenderedChannelRegex
         .Matches(html)
         .Select(match => NormalizeOptionalText(match.Groups["name"].Value))
         .Where(value => !string.IsNullOrWhiteSpace(value))
         .Select(value => value!)
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray();

      if(channelNames.Length == 0)
      {
         return false;
      }

      var categories = RenderedCategoryRegex
         .Matches(html)
         .Select(match => NormalizeOptionalText(
            WebUtility.HtmlDecode(match.Groups["category"].Value)
         ))
         .Where(value => !string.IsNullOrWhiteSpace(value))
         .Select(value => value!)
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray();
      var allowEmptyCategories = categories.Any(value =>
         string.Equals(
            value,
            "Övrig sport",
            StringComparison.OrdinalIgnoreCase
         )
      );

      categories = categories
         .Where(value =>
            !string.Equals(
               value,
               "Övrig sport",
               StringComparison.OrdinalIgnoreCase
            )
         )
         .ToArray();

      row = new RenderedRow(
         ExtractScheduleId(href),
         title,
         startsAt,
         null,
         categories,
         channelNames,
         allowEmptyCategories,
         ExtractRenderedValue(RenderedImageRegex, html, "image")
      );

      return true;
   }

   private bool TryCreateRenderedBroadcast(
      RenderedRow row,
      string channelName,
      DateTimeOffset endsAt,
      out Broadcast broadcast
   )
   {
      broadcast = null!;

      if(row.Categories.Count == 0 && !row.AllowEmptyCategories)
      {
         return false;
      }

      var normalizedChannelId = NormalizeChannelId(channelName);
      var normalizedChannelName = NormalizeBroadcastChannelName(
         channelName
      );
      var externalId = CreateExternalId(
         row.ScheduleId,
         "rendered",
         normalizedChannelId,
         row.StartsAt
      );
      var fingerprint = CreateFingerprint(
         normalizedChannelId,
         normalizedChannelName,
         row.StartsAt,
         row.Title
      );

      broadcast = new Broadcast(
         DeterministicGuid.Create($"broadcast:{fingerprint}"),
         sourceKey,
         externalId,
         fingerprint,
         normalizedChannelId,
         normalizedChannelName,
         row.Title,
         null,
         row.Categories,
         false,
         null,
         row.StartsAt,
         endsAt,
         SportDay.TimeZoneId,
         null,
         NormalizeOptionalText(row.ImageUrl)
      );

      return true;
   }

   private static DateTimeOffset? FindNextStart(
      IReadOnlyList<RenderedRow> rows,
      int index
   )
   {
      for(var nextIndex = index + 1; nextIndex < rows.Count; nextIndex++)
      {
         var nextStart = rows[nextIndex].StartsAt;

         if(nextStart > rows[index].StartsAt)
         {
            return nextStart;
         }
      }

      return null;
   }

   private IEnumerable<Broadcast> CreateBroadcasts(
      BroadcastScheduleItem scheduleItem
   )
   {
      if(string.IsNullOrWhiteSpace(scheduleItem.Title))
      {
         yield break;
      }

      var categories = GetCategories(scheduleItem);
      var allowEmptyCategories = HasGenericSportCategory(scheduleItem);

      foreach(var broadcast in scheduleItem.Broadcasts)
      {
         if(!TryCreateBroadcast(
            scheduleItem,
            categories,
            allowEmptyCategories,
            "broadcast",
            broadcast.Channel?.Slug,
            broadcast.Channel?.Name,
            broadcast.StartTime,
            broadcast.EndTime,
            broadcast.IsRerun,
            out var broadcastItem
         ))
         {
            continue;
         }

         yield return broadcastItem;
      }

      foreach(var playEpisode in scheduleItem.PlayEpisodes)
      {
         if(!TryCreateBroadcast(
            scheduleItem,
            categories,
            allowEmptyCategories,
            "stream",
            playEpisode.PlayProvider?.Slug,
            playEpisode.PlayProvider?.Name,
            playEpisode.StreamStart,
            playEpisode.StreamEnd,
            isReplay: false,
            out var broadcastItem
         ))
         {
            continue;
         }

         var streamLink = TryCreateStreamLink(
            playEpisode.PlayProvider
         );
         yield return broadcastItem with
         {
            StreamLinks = streamLink is null ? [] : [streamLink]
         };
      }
   }

   private static BroadcastStreamLink? TryCreateStreamLink(
      BroadcastChannelItem? provider
   )
   {
      var providerName = NormalizeOptionalText(provider?.Name);

      if(
         providerName is null ||
         !StreamLinkUrlNormalizer.TryNormalize(
            provider?.Url,
            out var normalizedUrl
         )
      )
      {
         return null;
      }

      return new BroadcastStreamLink(providerName, normalizedUrl);
   }

   private bool TryCreateBroadcast(
      BroadcastScheduleItem scheduleItem,
      IReadOnlyCollection<string> categories,
      bool allowEmptyCategories,
      string itemKind,
      string? channelId,
      string? channelName,
      long? startsAtValue,
      long? endsAtValue,
      bool isReplay,
      out Broadcast broadcast
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

      if(categories.Count == 0 && !allowEmptyCategories)
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
      var normalizedChannelName = NormalizeBroadcastChannelName(
         channelName
      );
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
         normalizedChannelId,
         normalizedChannelName,
         startsAt,
         title
      );

      broadcast = new Broadcast(
         DeterministicGuid.Create($"broadcast:{fingerprint}"),
         sourceKey,
         externalId,
         fingerprint,
         normalizedChannelId,
         normalizedChannelName,
         title,
         NormalizeOptionalText(scheduleItem.Description),
         categories,
         isReplay,
         null,
         startsAt,
         endsAt,
         SportDay.TimeZoneId,
         null,
         NormalizeOptionalText(
            scheduleItem.ImageLandscape ??
            scheduleItem.ImagePortrait
         )
      );

      return true;
   }

   private static IReadOnlyCollection<string> GetCategories(
      BroadcastScheduleItem scheduleItem
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
      .Where(value =>
         !string.Equals(
            value,
            "Övrig sport",
            StringComparison.OrdinalIgnoreCase
         )
      )
      .Select(value => value!)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();
   }

   private static bool HasGenericSportCategory(
      BroadcastScheduleItem scheduleItem
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
      .Any(value =>
         string.Equals(
            value,
            "Övrig sport",
            StringComparison.OrdinalIgnoreCase
         )
      );
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
      string channelId,
      string? channelName,
      DateTimeOffset startsAt,
      string title
   )
   {
      var localStart = TimeZoneInfo.ConvertTime(
         startsAt,
         StockholmTimeZone
      );
      var bucketMinute = localStart.Minute < 30 ? 0 : 30;
      var bucket = string.Format(
         CultureInfo.InvariantCulture,
         "{0:yyyy-MM-dd HH}:{1:00}",
         localStart,
         bucketMinute
      );
      var value = string.Join(
         "|",
         NormalizeFingerprintChannelKey(channelId, channelName),
         bucket,
         NormalizeKey(title)
      );
      var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));

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

   private static string NormalizeChannelId(string value)
   {
      var normalized = NormalizeText(value).ToLowerInvariant();
      var builder = new StringBuilder(normalized.Length);
      var previousWasSeparator = false;

      foreach(var character in normalized)
      {
         if(char.IsLetterOrDigit(character))
         {
            builder.Append(character);
            previousWasSeparator = false;
            continue;
         }

         if(previousWasSeparator)
         {
            continue;
         }

         builder.Append('-');
         previousWasSeparator = true;
      }

      return builder.ToString().Trim('-');
   }

   private static string NormalizeFingerprintChannelKey(
      string channelId,
      string? channelName
   )
   {
      var normalizedChannelId = NormalizeFingerprintChannelKey(channelId);

      if(FingerprintChannelAliases.TryGetValue(
         normalizedChannelId,
         out var canonicalChannelKey
      ))
      {
         return canonicalChannelKey;
      }

      if(!string.IsNullOrWhiteSpace(channelName))
      {
         var normalizedChannelName = NormalizeFingerprintChannelKey(
            channelName
         );

         if(FingerprintChannelAliases.TryGetValue(
            normalizedChannelName,
            out canonicalChannelKey
         ))
         {
            return canonicalChannelKey;
         }

         return normalizedChannelName;
      }

      return normalizedChannelId;
   }

   private static string NormalizeFingerprintChannelKey(string value)
   {
      var normalized = NormalizeChannelId(value);

      foreach(var suffix in FingerprintChannelSuffixes)
      {
         if(!normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
         {
            continue;
         }

         var suffixStartIndex = normalized.Length - suffix.Length;

         if(suffixStartIndex <= 0)
         {
            continue;
         }

         return normalized[..suffixStartIndex];
      }

      return normalized;
   }

   private static string NormalizeSourceKey(string? value)
   {
      return string.IsNullOrWhiteSpace(value)
         ? DefaultSourceKey
         : NormalizeText(value).ToLowerInvariant();
   }

   private static string? NormalizeOptionalText(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      return NormalizeText(value);
   }

   private static string? NormalizeBroadcastChannelName(string? value)
   {
      var channelName = NormalizeOptionalText(value);

      return channelName is null
         ? null
         : PrimaryCountry.NormalizeBroadcastChannelName(channelName);
   }

   private static string? ExtractRenderedValue(
      Regex regex,
      string html,
      string groupName
   )
   {
      var match = regex.Match(html);

      if(!match.Success)
      {
         return null;
      }

      return WebUtility.HtmlDecode(match.Groups[groupName].Value).Trim();
   }

   private static string ExtractScheduleId(string href)
   {
      if(Uri.TryCreate(href, UriKind.Absolute, out var uri))
      {
         return uri.AbsolutePath.Trim('/');
      }

      return href.Trim('/');
   }

   private static bool TryParseStockholmDateTime(
      string value,
      out DateTimeOffset dateTimeOffset
   )
   {
      dateTimeOffset = default;

      if(
         !DateTime.TryParseExact(
            value,
            DateDisplay.DateTimeMinutesFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var localDateTime
      )
   )
      {
         return false;
      }

      var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(
         DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified),
         StockholmTimeZone
      );
      dateTimeOffset = new DateTimeOffset(utcDateTime, TimeSpan.Zero);
      return true;
   }

   private static TimeZoneInfo GetStockholmTimeZone()
   {
      try
      {
         return TimeZoneInfo.FindSystemTimeZoneById(SportDay.TimeZoneId);
      }
      catch(TimeZoneNotFoundException)
      {
         return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
      }
      catch(InvalidTimeZoneException)
      {
         return TimeZoneInfo.Utc;
      }
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

   private static readonly TimeZoneInfo StockholmTimeZone =
      GetStockholmTimeZone();

   public sealed record BroadcastParseResult(
      IReadOnlyCollection<Broadcast> Broadcasts,
      int DuplicateBroadcastCount
   );

   private sealed class BroadcastState
   {
      [JsonPropertyName("sportPageSchedule")]
      public List<BroadcastScheduleItem> SportPageSchedule { get; set; } = [];
   }

   private sealed class BroadcastScheduleItem
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

      [JsonPropertyName("imageLandscape")]
      public string? ImageLandscape { get; set; }

      [JsonPropertyName("imagePortrait")]
      public string? ImagePortrait { get; set; }

      [JsonPropertyName("broadcasts")]
      public List<BroadcastItem> Broadcasts { get; set; } = [];

      [JsonPropertyName("playEpisodes")]
      public List<BroadcastPlayEpisodeItem> PlayEpisodes { get; set; } = [];
   }

   private sealed class BroadcastItem
   {
      [JsonPropertyName("startTime")]
      public long? StartTime { get; set; }

      [JsonPropertyName("endTime")]
      public long? EndTime { get; set; }

      [JsonPropertyName("isRerun")]
      public bool IsRerun { get; set; }

      [JsonPropertyName("channel")]
      public BroadcastChannelItem? Channel { get; set; }
   }

   private sealed class BroadcastPlayEpisodeItem
   {
      [JsonPropertyName("streamStart")]
      public long? StreamStart { get; set; }

      [JsonPropertyName("streamEnd")]
      public long? StreamEnd { get; set; }

      [JsonPropertyName("playProvider")]
      public BroadcastChannelItem? PlayProvider { get; set; }
   }

   private sealed class BroadcastChannelItem
   {
      [JsonPropertyName("name")]
      public string? Name { get; set; }

      [JsonPropertyName("slug")]
      public string? Slug { get; set; }

      [JsonPropertyName("url")]
      public string? Url { get; set; }
   }

   private sealed record RenderedRow(
      string ScheduleId,
      string Title,
      DateTimeOffset StartsAt,
      DateTimeOffset? EndsAt,
      IReadOnlyCollection<string> Categories,
      IReadOnlyCollection<string> ChannelNames,
      bool AllowEmptyCategories,
      string? ImageUrl
   );
}
