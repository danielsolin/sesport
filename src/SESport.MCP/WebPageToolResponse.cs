namespace SESport.MCP;

public sealed record WebPageToolResponse(
   string Title,
   string Url,
   DateTimeOffset? PublishedAt,
   IReadOnlyList<string> Headings,
   string MainText,
   bool HasBodyText,
   string? FetchErrorMessage = null,
   WebPageFetchErrorKind? FetchErrorKind = null,
   string? Fetcher = null,
   string? BrowserStrategy = null,
   IReadOnlyList<WebPageRelevantLink>? RelevantLinks = null,
   IReadOnlyList<WebPageImageCandidate>? RelevantImages = null
)
{
   public static WebPageToolResponse From(WebPageContent content)
   {
      ArgumentNullException.ThrowIfNull(content);

      return new WebPageToolResponse(
         content.Title,
         content.Url,
         content.PublishedAt,
         content.Headings,
         content.MainText,
         content.HasBodyText,
         content.FetchErrorMessage,
         content.FetchErrorKind,
         content.Fetcher,
         content.BrowserStrategy,
         content.RelevantLinks,
         content.RelevantImages
      );
   }
}
