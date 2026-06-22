using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

using SESport.AI.Interfaces;

namespace SESport.AI.Providers;

public sealed class WebPageContentClient : IWebPageContentClient
{
   internal const string CutoffMarker =
      WebPageContentFetchSupport.CutoffMarker;
   internal const int MaxResponseCharacters =
      WebPageContentFetchSupport.MaxResponseCharacters;

   private readonly HttpClient httpClient;
   private readonly ILogger<WebPageContentClient> logger;
   private readonly Func<Task<string>> browserUserAgentFetcher;
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      browserPageFetcher;
   private readonly Func<Uri, CancellationToken, Task<WebPageContent?>>
      curlPageFetcher;

   [ActivatorUtilitiesConstructor]
   public WebPageContentClient(HttpClient httpClient)
      : this(httpClient, null, null, null)
   {
   }

   public WebPageContentClient(
      HttpClient httpClient,
      Func<Uri, CancellationToken, Task<WebPageContent?>>? browserPageFetcher,
      ILogger<WebPageContentClient>? logger = null,
      Func<Task<string>>? browserUserAgentFetcher = null,
      Func<Uri, CancellationToken, Task<WebPageContent?>>? curlPageFetcher =
         null
   )
   {
      this.httpClient = httpClient;
      this.logger = logger ??
         Microsoft.Extensions.Logging.Abstractions.NullLogger<
            WebPageContentClient>.Instance;
      this.browserUserAgentFetcher = browserUserAgentFetcher ??
         WebPageContentFetchSupport.GetBrowserUserAgentAsync;
      this.browserPageFetcher = browserPageFetcher ??
         ((uri, cancellationToken) =>
            WebPageBrowserPageFetcher.FetchAsync(
               this.browserUserAgentFetcher,
               uri,
               cancellationToken
            ));
      this.curlPageFetcher = curlPageFetcher ??
         ((uri, cancellationToken) =>
            WebPageCurlPageFetcher.FetchAsync(
               this.logger,
               uri,
               cancellationToken
            ));
   }

   public async Task<WebPageContent?> FetchAsync(
      string url,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(url) ||
         !Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
      {
         return null;
      }

      var browserUserAgent = await this.browserUserAgentFetcher();
      using var request = new HttpRequestMessage(HttpMethod.Get, absoluteUrl);
      request.Headers.Accept.ParseAdd(
         "text/html,application/xhtml+xml,application/xml;q=0.9," +
         "image/avif,image/webp,*/*;q=0.8"
      );
      foreach(var header in WebPageContentFetchSupport.BuildBrowserLikeHeaders(
         browserUserAgent
      ))
      {
         request.Headers.TryAddWithoutValidation(
            header.Key,
            header.Value
         );
      }

      request.Headers.TryAddWithoutValidation("User-Agent", browserUserAgent);
      using var response = await httpClient.SendAsync(
         request,
         cancellationToken
      );

      if(WebPageContentFetchSupport.IsPdfResponse(response, absoluteUrl))
      {
         return await WebPagePdfPageFetcher.FetchAsync(
            response,
            absoluteUrl,
            cancellationToken
         );
      }

      try
      {
         return await this.browserPageFetcher(absoluteUrl, cancellationToken);
      }
      catch(WebPageFetchException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright failed for {Url}; falling back to HTML.",
            absoluteUrl
         );
         return await WebPageHtmlPageFetcher.FetchAsync(
            this.logger,
            this.curlPageFetcher,
            response,
            absoluteUrl,
            cancellationToken,
            exception.ErrorKind
         );
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(TimeoutException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright timed out for {Url}; falling back to HTML.",
            absoluteUrl
         );
         return await WebPageHtmlPageFetcher.FetchAsync(
            this.logger,
            this.curlPageFetcher,
            response,
            absoluteUrl,
            cancellationToken,
            WebPageFetchErrorKind.Timeout
         );
      }
      catch(PlaywrightException exception)
      {
         logger.LogWarning(
            exception,
            "Playwright failed for {Url}; falling back to HTML.",
            absoluteUrl
         );
         return await WebPageHtmlPageFetcher.FetchAsync(
            this.logger,
            this.curlPageFetcher,
            response,
            absoluteUrl,
            cancellationToken,
            WebPageFetchErrorKind.BrowserBlocked
         );
      }
   }

   internal static string BuildBrowserUserAgent(string browserVersion)
   {
      return WebPageContentFetchSupport.BuildBrowserUserAgent(browserVersion);
   }

   internal static string ApplyResponseCutoff(string text)
   {
      return WebPageContentFetchSupport.ApplyResponseCutoff(text);
   }

   internal static string? GetCountryDisplayName(string? countryCode)
   {
      return WebPageContentFetchSupport.GetCountryDisplayName(countryCode);
   }
}
