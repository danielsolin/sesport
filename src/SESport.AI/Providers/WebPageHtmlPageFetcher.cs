using Microsoft.Extensions.Logging;

namespace SESport.AI.Providers;

internal static class WebPageHtmlPageFetcher
{
   internal static async Task<WebPageContent?> FetchAsync(
      ILogger logger,
      Func<Uri, CancellationToken, Task<WebPageContent?>> curlPageFetcher,
      HttpResponseMessage response,
      Uri absoluteUrl,
      CancellationToken cancellationToken,
      WebPageFetchErrorKind? browserFailureKind = null
   )
   {
      try
      {
         var absoluteUrlString = absoluteUrl.ToString();
         var html = await response.Content.ReadAsStringAsync(
            cancellationToken
         );

         if(string.IsNullOrWhiteSpace(html))
         {
            logger.LogWarning(
               "HTML fallback had no body for {Url}.",
               absoluteUrl
            );
            return await TryCurlFallbackAsync(
               logger,
               curlPageFetcher,
               absoluteUrl,
               browserFailureKind,
               "HTML fallback had no body.",
               cancellationToken
            );
         }

         var title = WebPageContentFetchSupport.ExtractHtmlTitle(html);
         var text = WebPageContentFetchSupport
            .ExtractHtmlTextWithEmbeddedState(html);

         if(string.IsNullOrWhiteSpace(text))
         {
            logger.LogWarning(
               "HTML fallback produced no text for {Url}.",
               absoluteUrl
            );
            if(IsBlockedPage(title, text))
            {
               return await TryCurlFallbackAsync(
                  logger,
                  curlPageFetcher,
                  absoluteUrl,
                  browserFailureKind,
                  "HTML fallback produced no text.",
                  cancellationToken
               );
            }

            return WebPageContentFetchSupport.BuildFailureContent(
               absoluteUrl,
               title ?? absoluteUrlString,
               browserFailureKind,
               "HTML fallback produced no text."
            );
         }

         logger.LogInformation(
            "HTML fallback used for {Url}.",
            absoluteUrl
         );

         if(IsBlockedPage(title, text))
         {
            return await TryCurlFallbackAsync(
               logger,
               curlPageFetcher,
               absoluteUrl,
               browserFailureKind,
               "HTML fallback was blocked.",
               cancellationToken
            );
         }

         return new WebPageContent(
            title ?? absoluteUrlString,
            absoluteUrlString,
            null,
            [],
            WebPageContentFetchSupport.ApplyResponseCutoff(text),
            true,
            text
         );
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(Exception)
      {
         return WebPageContentFetchSupport.BuildFailureContent(
            absoluteUrl,
            null,
            browserFailureKind,
            "Unable to extract HTML fallback."
         );
      }
   }

   private static async Task<WebPageContent?> TryCurlFallbackAsync(
      ILogger logger,
      Func<Uri, CancellationToken, Task<WebPageContent?>> curlPageFetcher,
      Uri absoluteUrl,
      WebPageFetchErrorKind? browserFailureKind,
      string fallbackFailureMessage,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var curlContent = await curlPageFetcher(
            absoluteUrl,
            cancellationToken
         );

         if(curlContent is not null)
         {
            return curlContent;
         }
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(Exception exception)
      {
         logger.LogWarning(
            exception,
            "Curl fallback failed for {Url}.",
            absoluteUrl
         );
      }

      return WebPageContentFetchSupport.BuildFailureContent(
         absoluteUrl,
         null,
         browserFailureKind,
         fallbackFailureMessage
      );
   }

   private static bool IsBlockedPage(string? title, string text)
   {
      var combinedText =
         WebPageContentFetchSupport.NormalizeText($"{title} {text}");

      return combinedText.Contains(
         "access denied",
         StringComparison.OrdinalIgnoreCase
      ) ||
      combinedText.Contains(
         "you do not have permission to access",
         StringComparison.OrdinalIgnoreCase
      ) ||
      combinedText.Contains(
         "you don't have permission to access",
         StringComparison.OrdinalIgnoreCase
      ) ||
      combinedText.Contains(
         "errors edgesuite net",
         StringComparison.OrdinalIgnoreCase
      ) ||
      combinedText.Contains(
         "reference",
         StringComparison.OrdinalIgnoreCase
      );
   }
}
