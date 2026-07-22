namespace SESport.Web.Formatting;

public static class SourceDisplay
{
   private const int ExcerptPreviewLength = 20;

   public static string? FormatExcerpt(string? excerpt)
   {
      return excerpt?.Length > ExcerptPreviewLength
         ? excerpt[..ExcerptPreviewLength] + "..."
         : excerpt;
   }
}
