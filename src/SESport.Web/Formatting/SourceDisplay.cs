using SESport.Core.Sources;
using SESport.Data.Models;

using System.Globalization;

namespace SESport.Web.Formatting;

public static class SourceDisplay
{
   private const int ExcerptPreviewLength = 20;
   private static readonly StringComparer PrimaryCountryKindComparer =
      StringComparer.Create(
         CultureInfo.GetCultureInfo(PrimaryCountry.CultureName),
         true
      );

   public static string FormatKind(string kind)
   {
      return kind switch
      {
         SourceKinds.ActivityEvidence => "Aktivitet",
         SourceKinds.StreamLink => "Stream",
         SourceKinds.ParticipationEvidence => "Deltagande",
         SourceKinds.ParticipantStartEvidence => "Starttid",
         SourceKinds.ParticipantStarEvidence => "Stjärna",
         _ => "Källa"
      };
   }

   public static bool IsPubliclyVisibleSourceUrl(
      string? sourceUrl,
      IEnumerable<string> blockedSourceHosts
   )
   {
      if(
         string.IsNullOrWhiteSpace(sourceUrl) ||
         !Uri.TryCreate(
            sourceUrl,
            UriKind.Absolute,
            out var parsedUrl
         )
      )
      {
         return true;
      }

      var sourceHost = parsedUrl.Host.TrimEnd('.');

      return !blockedSourceHosts.Any(blockedHost =>
         IsBlockedHost(sourceHost, blockedHost)
      );
   }

   private static bool IsBlockedHost(
      string sourceHost,
      string blockedHost
   )
   {
      var normalizedBlockedHost = blockedHost.Trim().Trim('.');

      if(string.IsNullOrWhiteSpace(normalizedBlockedHost))
      {
         return false;
      }

      return string.Equals(
            sourceHost,
            normalizedBlockedHost,
            StringComparison.OrdinalIgnoreCase
         ) ||
         sourceHost.EndsWith(
            "." + normalizedBlockedHost,
            StringComparison.OrdinalIgnoreCase
         );
   }

   public static ActivitySourceListItem? FindStreamLinkForChannel(
      IEnumerable<ActivitySourceListItem> sources,
      string channel
   )
   {
      var normalizedChannel = channel.Trim();

      return sources.FirstOrDefault(source =>
         string.Equals(
            source.Kind,
            SourceKinds.StreamLink,
            StringComparison.OrdinalIgnoreCase
         ) &&
         string.Equals(
            source.Title?.Trim(),
            normalizedChannel,
            StringComparison.OrdinalIgnoreCase
         )
      );
   }

   public static string? FindChannelLinkUrlForChannel(
      IEnumerable<ActivitySourceListItem> sources,
      string channel,
      BroadcastChannelLinkCatalog catalog
   )
   {
      var streamSource = FindStreamLinkForChannel(sources, channel);
      return streamSource?.Url ??
         catalog.Find(channel)?.Url;
   }

   public static IReadOnlyList<ActivitySourceListItem>
      OrderDistinctByUrl(
         IEnumerable<ActivitySourceListItem> sources
      )
   {
      return sources
         .GroupBy(source => source.Url, StringComparer.OrdinalIgnoreCase)
         .Select(group => group
            .OrderBy(
               source => FormatKind(source.Kind),
               PrimaryCountryKindComparer
            )
            .ThenBy(source => source.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(source => source.Url, StringComparer.OrdinalIgnoreCase)
            .First()
         )
         .OrderBy(
            source => FormatKind(source.Kind),
            PrimaryCountryKindComparer
         )
         .ThenBy(source => source.Url, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   public static string? FormatExcerpt(string? excerpt)
   {
      return excerpt?.Length > ExcerptPreviewLength
         ? excerpt[..ExcerptPreviewLength] + "..."
         : excerpt;
   }
}
