namespace SESport.AI.WebPages;

/// <summary>
/// Shared content assessment. Every HTML-derived candidate, regardless of
/// transport (direct HTTP, Playwright, or curl), is assessed by this single
/// service so identical evidence receives identical classification.
/// </summary>
internal static class WebPageContentAssessor
{
   /// <param name="title">Extracted page title.</param>
   /// <param name="visibleText">
   /// Raw visible text (template artifacts still present). Used for
   /// block, not-found, and placeholder signals.
   /// </param>
   /// <param name="textContent">
   /// Cleaned extracted text, including embedded state. This is the text
   /// that would be returned to the caller.
   /// </param>
   /// <param name="renderWarning">Incomplete-content warning, if any.</param>
   /// <param name="headingCount">Number of extracted headings.</param>
   /// <param name="hasPublishedAt">
   /// Whether publication metadata was found.
   /// </param>
   /// <param name="relevantLinkCount">Number of relevant links.</param>
   /// <param name="blockSource">Which signature set applies.</param>
   internal static WebPageAssessment Assess(
      string? title,
      string visibleText,
      string textContent,
      string? renderWarning,
      int headingCount,
      bool hasPublishedAt,
      int relevantLinkCount,
      WebPageBlockSource blockSource
   )
   {
      var softNotFoundSignature = WebPageBlockDetection
         .FindSoftErrorSignature(title, visibleText);

      if(softNotFoundSignature is not null)
      {
         return new WebPageAssessment(
            WebPageContentClassification.NotFound,
            $"not-found marker: {softNotFoundSignature}",
            null,
            softNotFoundSignature
         );
      }

      var blockSignature = WebPageBlockDetection.FindBlockedSignature(
         title,
         visibleText,
         blockSource
      );

      if(blockSignature is not null)
      {
         return new WebPageAssessment(
            WebPageContentClassification.Blocked,
            $"block marker: {blockSignature}",
            blockSignature,
            null
         );
      }

      var cleanedVisibleText =
         WebPageContentFetchSupport.RemoveTemplateArtifacts(visibleText);

      if(string.IsNullOrWhiteSpace(textContent) &&
         string.IsNullOrWhiteSpace(cleanedVisibleText))
      {
         return new WebPageAssessment(
            WebPageContentClassification.Empty,
            "no extractable text",
            null,
            null
         );
      }

      var visibleLength = cleanedVisibleText.Length;
      var hasStructure =
         headingCount > 0 || relevantLinkCount > 0 || hasPublishedAt;

      if(renderWarning is not null &&
         visibleLength <
            WebPageFetchDefaults.RichContentMinimumCharacters)
      {
         return new WebPageAssessment(
            WebPageContentClassification.NeedsRendering,
            $"incomplete content signals ({renderWarning})",
            null,
            null
         );
      }

      if(visibleLength >=
         WebPageFetchDefaults.RichContentMinimumCharacters)
      {
         return WebPageAssessment.Usable;
      }

      if(visibleLength >=
         WebPageFetchDefaults.ShortPageUsableMinimumCharacters &&
         hasStructure)
      {
         return WebPageAssessment.Usable;
      }

      return new WebPageAssessment(
         WebPageContentClassification.Partial,
         "short content without strong structure",
         null,
         null
      );
   }
}
