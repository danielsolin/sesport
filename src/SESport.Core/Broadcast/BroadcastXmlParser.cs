using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SESport.Core.Broadcast;

public sealed partial class BroadcastXmlParser
{
   private const string DefaultSourceKey = "iptv-epg-se";

   public async Task<IReadOnlyCollection<Broadcast>> ParseAsync(
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
      var broadcasts = new List<Broadcast>();

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

   private static Broadcast? TryCreateBroadcast(
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

      var externalId = CreateExternalId(channelId, startsAt, title);
      var fingerprint = CreateFingerprint(
         DefaultSourceKey,
         channelId,
         startsAt,
         title
      );

      channels.TryGetValue(channelId, out var channelName);

      return new Broadcast(
         DeterministicGuid.Create($"broadcast:{fingerprint}"),
         DefaultSourceKey,
         externalId,
         fingerprint,
         channelId,
         channelName,
         title,
         description,
         storedCategories,
         false,
         null,
         startsAt,
         endsAt,
         SportDay.TimeZoneId,
         programme.ToString(SaveOptions.DisableFormatting),
         null
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
         category.Equals("Sportmagasin", StringComparison.OrdinalIgnoreCase) ||
         category.Equals("Dokumentär", StringComparison.OrdinalIgnoreCase));
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
      var channelName = NormalizeText(value);

      return PrimaryCountry.NormalizeBroadcastChannelName(channelName);
   }

   [GeneratedRegex(@"\s+")]
   private static partial Regex WhitespaceRegex();

}
