namespace SESport.AI.WebPages;

/// <summary>
/// One extracted HTML content candidate. Built by the shared extractor for
/// every transport and assessed by <see cref="WebPageContentAssessor"/>.
/// </summary>
internal sealed record WebPageHtmlCandidate(
   Uri Url,
   string Title,
   IReadOnlyList<string> Headings,
   DateTimeOffset? PublishedAt,
   string VisibleText,
   string TextContent,
   string? RenderWarning,
   IReadOnlyList<WebPageRelevantLink> RelevantLinks,
   IReadOnlyList<WebPageImageCandidate> RelevantImages
)
{
   internal static WebPageHtmlCandidate FromHtml(
      string html,
      Uri baseUrl
   )
   {
      return new WebPageHtmlCandidate(
         baseUrl,
         WebPageContentFetchSupport.ExtractHtmlTitle(html) ?? "",
         WebPageContentFetchSupport.ExtractHtmlHeadings(html),
         WebPageContentFetchSupport.ExtractPublishedAt(html),
         WebPageContentFetchSupport.ExtractHtmlText(html),
         WebPageContentFetchSupport.RemoveTemplateArtifacts(
            WebPageContentFetchSupport
               .ExtractHtmlTextWithEmbeddedState(html)
         ),
         WebPageContentFetchSupport.DetectIncompleteContentWarning(
            WebPageContentFetchSupport.ExtractHtmlText(html)
         ),
         WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
            html,
            baseUrl
         ),
         WebPageContentFetchSupport.ExtractRelevantImagesFromHtml(
            html,
            baseUrl
         )
      );
   }

   internal static WebPageHtmlCandidate FromRendered(
      string fullHtml,
      string bodyHtml,
      string title,
      IReadOnlyList<WebPageImageCandidate> relevantImages,
      Uri baseUrl
   )
   {
      return new WebPageHtmlCandidate(
         baseUrl,
         title,
         WebPageContentFetchSupport.ExtractHtmlHeadings(bodyHtml),
         WebPageContentFetchSupport.ExtractPublishedAt(fullHtml),
         WebPageContentFetchSupport.ExtractHtmlText(bodyHtml),
         WebPageContentFetchSupport.RemoveTemplateArtifacts(
            WebPageContentFetchSupport
               .ExtractHtmlTextWithEmbeddedState(bodyHtml)
         ),
         WebPageContentFetchSupport.DetectIncompleteContentWarning(
            WebPageContentFetchSupport.ExtractHtmlText(bodyHtml)
         ),
         WebPageContentFetchSupport.MergeRelevantLinks(
            WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
               fullHtml,
               baseUrl
            ),
            WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
               bodyHtml,
               baseUrl
            )
         ),
         relevantImages
      );
   }

   internal WebPageAssessment Assess(WebPageBlockSource blockSource)
   {
      return WebPageContentAssessor.Assess(
         Title,
         VisibleText,
         TextContent,
         RenderWarning,
         Headings.Count,
         PublishedAt is not null,
         RelevantLinks.Count,
         blockSource
      );
   }
}
