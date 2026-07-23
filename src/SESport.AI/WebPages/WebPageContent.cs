namespace SESport.AI.WebPages;

public sealed record WebPageContent(
   string Title,
   string Url,
   DateTimeOffset? PublishedAt,
   IReadOnlyList<string> Headings,
   string MainText,
   bool HasBodyText,
   string MainTextFull = "",
   string? FetchErrorMessage = null,
   WebPageFetchErrorKind? FetchErrorKind = null,
   string? Fetcher = null,
   IReadOnlyList<WebPageRelevantLink>? RelevantLinks = null,
   IReadOnlyList<WebPageImageCandidate>? RelevantImages = null
);
