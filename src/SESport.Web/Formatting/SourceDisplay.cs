using System.Globalization;

using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Web.Formatting;

public static class SourceDisplay
{
   private const int ExcerptPreviewLength = 20;

   public static string FormatKind(string kind)
   {
      return kind switch
      {
         SourceKinds.ActivityEvidence => "Aktivitet",
         SourceKinds.ParticipationEvidence => "Deltagande",
         SourceKinds.ParticipantStartEvidence => "Starttid",
         SourceKinds.ParticipantStarEvidence => "Stjärna",
         _ => "Källa"
      };
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
