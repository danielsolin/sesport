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

      if(string.IsNullOrWhiteSpace(url))
      {
         error = "Missing page URL.";
         return false;
      }

      if(url.Length > 2048)
      {
         error = "Page URL is too long.";
         return false;
      }

      if(!Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
      {
         error = "Invalid page URL.";
         return false;
      }

      if(!string.Equals(
         absoluteUrl.Scheme,
         Uri.UriSchemeHttp,
         StringComparison.OrdinalIgnoreCase
      ) &&
         !string.Equals(
            absoluteUrl.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase
         ))
      {
         error = "Page URL must use http or https.";
         return false;
      }

      if(string.IsNullOrWhiteSpace(absoluteUrl.Host))
      {
         error = "Page URL is missing a host.";
         return false;
      }

      if(IsBlockedHost(absoluteUrl.Host))
      {
         error = "Page URL host is not allowed.";
         return false;
      }

      normalizedUrl = absoluteUrl.ToString();
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

   private static bool IsBlockedHost(string host)
   {
      return string.Equals(
         host,
         "localhost",
         StringComparison.OrdinalIgnoreCase
      ) || string.Equals(
         host,
         "127.0.0.1",
         StringComparison.OrdinalIgnoreCase
      ) || string.Equals(
         host,
         "::1",
         StringComparison.OrdinalIgnoreCase
      );
   }
}
