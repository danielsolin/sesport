using Microsoft.Extensions.Logging;

namespace SESport.AI.WebPages;

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
         var html = await response.Content.ReadAsStringAsync(
            cancellationToken
         );

         return await FetchHtmlAsync(
            logger,
            curlPageFetcher,
            html,
            absoluteUrl,
            cancellationToken,
            browserFailureKind
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
            "Unable to extract HTML fallback.",
            "html"
         );
      }
   }

   internal static async Task<WebPageContent?> FetchHtmlAsync(
      ILogger logger,
      Func<Uri, CancellationToken, Task<WebPageContent?>> curlPageFetcher,
      string html,
      Uri absoluteUrl,
      CancellationToken cancellationToken,
      WebPageFetchErrorKind? browserFailureKind = null
   )
   {
      try
      {
         var absoluteUrlString = absoluteUrl.ToString();

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
            var blockedSignature = WebPageBlockDetection.FindBlockedSignature(
               title,
               text,
               WebPageBlockSource.HtmlFallback
            );
            if(blockedSignature is not null)
            {
               logger.LogWarning(
                  "HTML fallback blocked for {Url} by signature {Signature}.",
                  absoluteUrl,
                  blockedSignature
               );
            }

            return await TryCurlFallbackAsync(
               logger,
               curlPageFetcher,
               absoluteUrl,
               browserFailureKind,
               blockedSignature is null
                  ? "HTML fallback produced no text."
                  : "HTML fallback was blocked.",
               cancellationToken
            );
         }

         logger.LogInformation(
            "HTML fallback used for {Url}.",
            absoluteUrl
         );

         if(WebPageBlockDetection.IsBlocked(
            title,
            text,
            WebPageBlockSource.HtmlFallback
         ))
         {
            var blockedSignature = WebPageBlockDetection
               .FindBlockedSignature(
                  title,
                  text,
                  WebPageBlockSource.HtmlFallback
               );

            logger.LogWarning(
               "HTML fallback blocked for {Url} by signature {Signature}.",
               absoluteUrl,
               blockedSignature ?? "<unknown>"
            );

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
            WebPageContentFetchSupport.ExtractHtmlHeadings(html),
            WebPageContentFetchSupport.ApplyResponseCutoff(text),
            true,
            text,
            Fetcher: "html",
            RelevantLinks: WebPageContentFetchSupport
               .ExtractRelevantLinksFromHtml(html, absoluteUrl),
            RenderWarning: WebPageContentFetchSupport
               .DetectIncompleteContentWarning(text)
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
            "Unable to extract HTML fallback.",
            "html"
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
         fallbackFailureMessage,
         "curl"
      );
   }

}
