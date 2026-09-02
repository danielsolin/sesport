namespace SESport.AI.WebPages;

/// <summary>
/// Result of PDF text extraction. Kept separate from
/// <see cref="WebPageContent"/> because the orchestrator may retry the
/// download through another transport before deciding the final result.
/// </summary>
internal sealed record PdfExtractionResult(
   bool Success,
   string? Text,
   string? Title,
   string? Error
)
{
   internal static PdfExtractionResult Succeeded(string text, string? title)
   {
      return new PdfExtractionResult(true, text, title, null);
   }

   internal static PdfExtractionResult Failed(string error)
   {
      return new PdfExtractionResult(false, null, null, error);
   }
}
