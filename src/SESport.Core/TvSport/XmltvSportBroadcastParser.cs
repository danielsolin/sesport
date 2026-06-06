using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SESport.Core.TvSport;

public sealed partial class XmltvSportBroadcastParser
{
   private const string DefaultSourceKey = "iptv-epg-se";
   private const string TimeZoneId = "Europe/Stockholm";

   public async Task<IReadOnlyCollection<TvSportBroadcast>> ParseAsync(
      Stream stream,
      CancellationToken cancellationToken
   )
   {
      var settings = new XmlReaderSettings
      {
         DtdProcessing = DtdProcessing.Ignore,
         Async = true
      };

      using var reader = XmlReader.Create(stream, settings);
      var channels = new Dictionary<string, string>(
         StringComparer.OrdinalIgnoreCase
      );
      var broadcasts = new List<TvSportBroadcast>();

      while(await reader.ReadAsync())
      {
         cancellationToken.ThrowIfCancellationRequested();

         if(reader.NodeType != XmlNodeType.Element)
         {
            continue;
         }

         if(reader.Name == "channel")
         {
            var channel = XElement.Parse(await reader.ReadOuterXmlAsync());
            AddChannel(channels, channel);
            continue;
         }

         if(reader.Name != "programme")
         {
            continue;
         }

         var programme = XElement.Parse(await reader.ReadOuterXmlAsync());
         var broadcast = TryCreateBroadcast(channels, programme);

         if(broadcast is not null)
         {
            broadcasts.Add(broadcast);
         }
      }

      return broadcasts;
   }

   private static void AddChannel(
      IDictionary<string, string> channels,
      XElement channel
   )
   {
      var id = channel.Attribute("id")?.Value;

      if(string.IsNullOrWhiteSpace(id))
      {
         return;
      }

      var displayName = channel
         .Elements("display-name")
         .Select(element => NormalizeChannelName(element.Value))
         .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

      if(!string.IsNullOrWhiteSpace(displayName))
      {
         channels[id] = displayName;
      }
   }

   private static TvSportBroadcast? TryCreateBroadcast(
      IReadOnlyDictionary<string, string> channels,
      XElement programme
   )
   {
      var categories = programme
         .Elements("category")
         .Select(element => NormalizeText(element.Value))
         .Where(value => !string.IsNullOrWhiteSpace(value))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();

      if(!IsSportProgramme(categories))
      {
         return null;
      }

      if(ShouldSkipProgramme(categories))
      {
         return null;
      }

      var storedCategories = categories
         .Select(NormalizeCategory)
         .Where(value => !ShouldSkipStoredCategory(value))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();

      if(storedCategories.Count == 0)
      {
         return null;
      }

      var channelId = programme.Attribute("channel")?.Value;
      var startValue = programme.Attribute("start")?.Value;
      var stopValue = programme.Attribute("stop")?.Value;
      var title = GetFirstElementValue(programme, "title");

      if(
         string.IsNullOrWhiteSpace(channelId) ||
         string.IsNullOrWhiteSpace(startValue) ||
         string.IsNullOrWhiteSpace(stopValue) ||
         string.IsNullOrWhiteSpace(title)
      )
      {
         return null;
      }

      var startsAt = ParseXmltvDateTime(startValue);
      var endsAt = ParseXmltvDateTime(stopValue);

      if(endsAt <= startsAt)
      {
         return null;
      }

      var description = GetFirstElementValue(programme, "desc");

      if(ShouldSkipDescription(description))
      {
         return null;
      }

      // Disabled for now: the current description heuristics mark too many
      // real live broadcasts as replays.
      var replayMetadata = (
         IsReplay: false,
         OriginalAirDate: (DateOnly?)null
      );
      // var replayMetadata = ParseReplayMetadata(description);
      var externalId = CreateExternalId(channelId, startsAt, title);
      var fingerprint = CreateFingerprint(
         DefaultSourceKey,
         channelId,
         startsAt,
         title
      );

      channels.TryGetValue(channelId, out var channelName);

      return new TvSportBroadcast(
         DeterministicGuid.Create($"tv-sport-broadcast:{fingerprint}"),
         DefaultSourceKey,
         externalId,
         fingerprint,
         channelId,
         channelName,
         title,
         description,
         storedCategories,
         replayMetadata.IsReplay,
         replayMetadata.OriginalAirDate,
         startsAt,
         endsAt,
         TimeZoneId,
         programme.ToString(SaveOptions.DisableFormatting)
      );
   }

   private static bool IsSportProgramme(
      IReadOnlyCollection<string> categories
   )
   {
      return categories.Any(category =>
         category.Equals("Sport", StringComparison.OrdinalIgnoreCase) ||
         category.Equals("Sports", StringComparison.OrdinalIgnoreCase));
   }

   private static bool ShouldSkipProgramme(
      IReadOnlyCollection<string> categories
   )
   {
      return categories.Any(category =>
         category.Equals("Sportmagasin", StringComparison.OrdinalIgnoreCase));
   }

   private static string NormalizeCategory(string category)
   {
      return category.Equals("Motor sport", StringComparison.OrdinalIgnoreCase)
         ? "Motorsport"
         : category;
   }

   private static bool ShouldSkipStoredCategory(string category)
   {
      return category.Equals("Sport", StringComparison.OrdinalIgnoreCase) ||
         category.Equals("Sports", StringComparison.OrdinalIgnoreCase) ||
         category.Equals(
            "Klubba och Bollspel",
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static bool ShouldSkipDescription(string? description)
   {
      if(string.IsNullOrWhiteSpace(description))
      {
         return false;
      }

      return description.Contains(
         "höjdpunkter",
         StringComparison.OrdinalIgnoreCase
      ) ||
         description.Contains("highlights", StringComparison.OrdinalIgnoreCase);
   }

   private static (
      bool IsReplay,
      DateOnly? OriginalAirDate
   ) ParseReplayMetadata(
      string? description
   )
   {
      if(string.IsNullOrWhiteSpace(description))
      {
         return (false, null);
      }

      var match = OriginalAirDateRegex().Match(description);

      if(!match.Success)
      {
         return (ProducedYearRegex().IsMatch(description), null);
      }

      var day = int.Parse(
         match.Groups["day"].Value,
         CultureInfo.InvariantCulture
      );
      var month = int.Parse(
         match.Groups["month"].Value,
         CultureInfo.InvariantCulture
      );
      var year = 2000 + int.Parse(
         match.Groups["year"].Value,
         CultureInfo.InvariantCulture
      );

      return (true, new DateOnly(year, month, day));
   }

   private static string? GetFirstElementValue(
      XElement element,
      string elementName
   )
   {
      return element
         .Elements(elementName)
         .Select(child => NormalizeText(child.Value))
         .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
   }

   private static DateTimeOffset ParseXmltvDateTime(string value)
   {
      var compactOffset = value.Trim();

      if(
         compactOffset.Length >= 5 &&
         (compactOffset[^5] == '+' || compactOffset[^5] == '-')
      )
      {
         compactOffset = compactOffset.Insert(compactOffset.Length - 2, ":");
      }

      return DateTimeOffset.ParseExact(
         compactOffset,
         "yyyyMMddHHmmss zzz",
         CultureInfo.InvariantCulture
      );
   }

   private static string CreateExternalId(
      string channelId,
      DateTimeOffset startsAt,
      string title
   )
   {
      return $"{channelId}:{startsAt:yyyyMMddHHmmss}:{NormalizeKey(title)}";
   }

   private static string CreateFingerprint(
      string sourceKey,
      string channelId,
      DateTimeOffset startsAt,
      string title
   )
   {
      var value = string.Join(
         "|",
         sourceKey,
         channelId.Trim().ToUpperInvariant(),
         startsAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
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
      var unescapedValue = value.Replace("\\u0026", "&");

      return WhitespaceRegex().Replace(unescapedValue.Trim(), " ");
   }

   private static string NormalizeChannelName(string value)
   {
      const string swedishPrefix = "SE - ";
      var channelName = NormalizeText(value);

      return channelName.StartsWith(
         swedishPrefix,
         StringComparison.OrdinalIgnoreCase
      )
         ? channelName[swedishPrefix.Length..]
         : channelName;
   }

   [GeneratedRegex(@"\s+")]
   private static partial Regex WhitespaceRegex();

   private const string OriginalAirDatePattern =
      @"\((?:\d{1,2}-)?(?<day>\d{1,2})/"
      + @"(?<month>\d{1,2})-(?<year>\d{2})\)";

   [GeneratedRegex(OriginalAirDatePattern)]
   private static partial Regex OriginalAirDateRegex();

   [GeneratedRegex(@"Producerat år", RegexOptions.IgnoreCase)]
   private static partial Regex ProducedYearRegex();
}
