namespace SESport.AI.WebPages;

/// <summary>
/// Direct HTTP transport. Redirect following is manual so every hop target
/// is validated by <see cref="WebPageUrlPolicy"/> before the request is
/// sent.
/// </summary>
internal static class WebPageHttpTransport
{
   internal static async Task<WebPageHttpResponse> SendAsync(
      HttpClient httpClient,
      Uri url,
      string browserUserAgent,
      CancellationToken cancellationToken
   )
   {
      var currentUrl = url;

      for(var hop = 0;
         hop <= WebPageFetchDefaults.MaxRedirectHops;
         hop++)
      {
         try
         {
            using var request =
               CreateRequestMessage(currentUrl, browserUserAgent);
            using var response = await httpClient.SendAsync(
               request,
               cancellationToken
            );

            var statusCode = (int)response.StatusCode;

            if(IsRedirectStatus(statusCode))
            {
               var location = response.Headers.Location;

               if(location is null)
               {
                  return WebPageHttpResponse.Failure(
                     url,
                     $"Redirect response {statusCode} had no Location."
                  );
               }

               var target = new Uri(currentUrl, location);

               if(!WebPageUrlPolicy.TryValidate(
                  target.ToString(),
                  out var validatedTarget,
                  out var policyError
               ))
               {
                  return WebPageHttpResponse.RedirectBlocked(
                     url,
                     target,
                     $"Redirect target rejected: {policyError}"
                  );
               }

               currentUrl = validatedTarget;
               continue;
            }

            var body = await response.Content.ReadAsByteArrayAsync(
               cancellationToken
            );

            return new WebPageHttpResponse(
               url,
               currentUrl,
               currentUrl != url,
               statusCode,
               response.Content.Headers.ContentType?.MediaType,
               body,
               null,
               null
            );
         }
         catch(OperationCanceledException)
            when(cancellationToken.IsCancellationRequested)
         {
            throw;
         }
         catch(Exception exception)
         {
            return WebPageHttpResponse.Failure(
               url,
               WebPageFetchLogging.SummarizeException(exception)
            );
         }
      }

      return WebPageHttpResponse.Failure(
         url,
         $"Too many redirects (limit " +
         $"{WebPageFetchDefaults.MaxRedirectHops})."
      );
   }

   internal static HttpRequestMessage CreateRequestMessage(
      Uri url,
      string browserUserAgent
   )
   {
      var request = new HttpRequestMessage(HttpMethod.Get, url);
      request.Headers.Accept.ParseAdd(
         WebPageFetchDefaults.BrowserAcceptHeader
      );
      foreach(var header in WebPageContentFetchSupport
         .BuildBrowserLikeHeaders(browserUserAgent))
      {
         request.Headers.TryAddWithoutValidation(
            header.Key,
            header.Value
         );
      }

      request.Headers.TryAddWithoutValidation(
         "User-Agent",
         browserUserAgent
      );
      return request;
   }

   internal static bool IsRedirectStatus(int statusCode)
   {
      return statusCode is 300 or 301 or 302 or 303 or 307 or 308;
   }
}
