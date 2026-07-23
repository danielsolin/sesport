using SESport.AI.WebPages;

namespace SESport.AI.Llama;

internal static class LlamaPageToolSupport
{
   public static bool TryValidatePageUrl(
      string url,
      out string normalizedUrl,
      out string error
   )
   {
      normalizedUrl = "";
      error = "";

      if(!WebPageUrlPolicy.TryValidate(
         url,
         out var absoluteUrl,
         out error
      ))
      {
         return false;
      }

      normalizedUrl = absoluteUrl.AbsoluteUri;
      return true;
   }

   public static string? TryGetCachedPageFetcher(
      IReadOnlyDictionary<string, WebPageContent?> pageContentCache,
      string url
   )
   {
      return pageContentCache.TryGetValue(url, out var cachedPage)
         ? cachedPage?.Fetcher
         : null;
   }

   public static string FormatFetchErrorText(
      LlamaPageTarget pageTarget,
      string? fetchErrorMessage,
      WebPageFetchErrorKind? fetchErrorKind
   )
   {
      var message = string.IsNullOrWhiteSpace(fetchErrorMessage)
         ? $"Unable to fetch page content from {pageTarget.Url}."
         : fetchErrorMessage.Trim();

      return LlamaPageToolFormatter.FormatPageContentText(
         pageTarget.ReferenceLabel,
         pageTarget.ReferenceValue,
         pageTarget.Title,
         pageTarget.Url,
         pageTarget.SearchSnippet,
         null,
         null,
         null,
         null,
         null,
         null,
         message,
         fetchErrorKind
      );
   }
}
