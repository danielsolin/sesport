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
      var visitedUrls = new HashSet<string>(StringComparer.Ordinal)
      {
         WebPageUrlPolicy.GetCanonicalCacheKey(currentUrl)
      };

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
               HttpCompletionOption.ResponseHeadersRead,
               cancellationToken
            );

            var statusCode = (int)response.StatusCode;

            if(IsRedirectStatus(statusCode))
            {
               if(hop >= WebPageFetchDefaults.MaxRedirectHops)
               {
                  return WebPageHttpResponse.Failure(
                     url,
                     "Too many redirects " +
                        $"(limit {WebPageFetchDefaults.MaxRedirectHops}).",
                     currentUrl
                  );
               }

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

               if(!visitedUrls.Add(
                  WebPageUrlPolicy.GetCanonicalCacheKey(validatedTarget)
               ))
               {
                  return WebPageHttpResponse.Failure(
                     url,
                     "Redirect loop detected.",
                     currentUrl
                  );
               }

               currentUrl = validatedTarget;
               continue;
            }

            var contentLength = response.Content.Headers.ContentLength;
            if(contentLength > WebPageFetchDefaults.MaximumResponseBytes)
            {
               return WebPageHttpResponse.ResponseTooLarge(
                  url,
                  currentUrl,
                  statusCode,
                  response.Content.Headers.ContentType?.MediaType
               );
            }

            var bodyResult = await ReadBodyAsync(
               response.Content,
               cancellationToken
            );

            if(bodyResult.IsTooLarge)
            {
               return WebPageHttpResponse.ResponseTooLarge(
                  url,
                  currentUrl,
                  statusCode,
                  response.Content.Headers.ContentType?.MediaType
               );
            }

            return new WebPageHttpResponse(
               url,
               currentUrl,
               currentUrl != url,
               statusCode,
               response.Content.Headers.ContentType?.MediaType,
               bodyResult.Body,
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
         $"{WebPageFetchDefaults.MaxRedirectHops}).",
         currentUrl
      );
   }

   private static async Task<BodyReadResult> ReadBodyAsync(
      HttpContent content,
      CancellationToken cancellationToken
   )
   {
      await using var stream = await content.ReadAsStreamAsync(
         cancellationToken
      );
      using var body = new MemoryStream();
      var buffer = new byte[81920];

      while(true)
      {
         var bytesRead = await stream.ReadAsync(
            buffer,
            cancellationToken
         );

         if(bytesRead == 0)
         {
            return new BodyReadResult(body.ToArray(), false);
         }

         if(body.Length > WebPageFetchDefaults.MaximumResponseBytes -
            bytesRead)
         {
            return new BodyReadResult([], true);
         }

         body.Write(buffer, 0, bytesRead);
      }
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

   private sealed record BodyReadResult(byte[] Body, bool IsTooLarge);
}
