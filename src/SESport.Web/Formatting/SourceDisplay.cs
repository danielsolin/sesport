using SESport.Core.Sources;
using SESport.Data.Models;
using System.Globalization;

namespace SESport.Web.Formatting;

public static class SourceDisplay
{
   private const int ExcerptPreviewLength = 20;

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

   public static IReadOnlyList<ActivitySourceListItem>
      OrderDistinctByUrl(
         IEnumerable<ActivitySourceListItem> sources
      )
   {
      var kindComparer = StringComparer.Create(
         CultureInfo.GetCultureInfo(PrimaryCountry.CultureName),
         true
      );

      return sources
         .GroupBy(source => source.Url, StringComparer.OrdinalIgnoreCase)
         .Select(group => group
            .OrderBy(
               source => FormatKind(source.Kind),
               kindComparer
            )
            .ThenBy(source => source.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(source => source.Url, StringComparer.OrdinalIgnoreCase)
            .First()
         )
         .OrderBy(
            source => FormatKind(source.Kind),
            kindComparer
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
