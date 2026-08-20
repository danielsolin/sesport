using SESport.Core.Sources;

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

   public static string? FormatExcerpt(string? excerpt)
   {
      return excerpt?.Length > ExcerptPreviewLength
         ? excerpt[..ExcerptPreviewLength] + "..."
         : excerpt;
   }
}
