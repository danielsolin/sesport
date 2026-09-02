using System.Diagnostics;

namespace SESport.AI.WebPages;

/// <summary>
/// Curl transport. Produces the same structured evidence as the direct
/// HTTP transport. Redirect following is manual so every hop target is
/// validated by <see cref="WebPageUrlPolicy"/>. curl keeps its own default
/// User-Agent on purpose: it is an independent transport, not a browser
/// substitute. The body is written to a temporary file so binary content
/// (PDFs) survives intact.
/// </summary>
internal static class WebPageCurlTransport
{
   private const string EndMarker = "__SESPORT_CURL_END__";
   private const int CurlFileSizeExceededExitCode = 63;

   internal static async Task<WebPageHttpResponse> SendAsync(
      Uri url,
      int maxTimeSeconds,
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
         var result = await RunCurlOnceAsync(
            currentUrl,
            maxTimeSeconds,
            cancellationToken
         );

         if(result.ErrorKind == WebPageFetchErrorKind.ResponseTooLarge)
         {
            return WebPageHttpResponse.ResponseTooLarge(
               url,
               currentUrl,
               result.StatusCode,
               result.ContentType
            );
         }

         if(result.TransportError is not null)
         {
            return WebPageHttpResponse.Failure(
               url,
               result.TransportError
            );
         }

         var statusCode = result.StatusCode!.Value;

         if(WebPageHttpTransport.IsRedirectStatus(statusCode))
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

            var location = FindHeaderValue(result.Headers, "location");

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

         return new WebPageHttpResponse(
            url,
            currentUrl,
            currentUrl != url,
            statusCode,
            result.ContentType,
            result.Body,
            null,
            null
         );
      }

      return WebPageHttpResponse.Failure(
         url,
         $"Too many redirects (limit " +
         $"{WebPageFetchDefaults.MaxRedirectHops}).",
         currentUrl
      );
   }

   private static async Task<CurlRunResult> RunCurlOnceAsync(
      Uri url,
      int maxTimeSeconds,
      CancellationToken cancellationToken
   )
   {
      var bodyPath = Path.Combine(
         Path.GetTempPath(),
         $"sesport-curl-{Guid.NewGuid():N}.bin"
      );

      Process? process = null;

      try
      {
         var processStartInfo = new ProcessStartInfo
         {
            FileName = "curl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
         };

         processStartInfo.ArgumentList.Add("--silent");
         processStartInfo.ArgumentList.Add("--show-error");
         processStartInfo.ArgumentList.Add("--proto");
         processStartInfo.ArgumentList.Add("=http,https");
         processStartInfo.ArgumentList.Add("--compressed");
         processStartInfo.ArgumentList.Add("--max-time");
         processStartInfo.ArgumentList.Add(maxTimeSeconds.ToString());
         processStartInfo.ArgumentList.Add("--max-filesize");
         processStartInfo.ArgumentList.Add(
            WebPageFetchDefaults.MaximumResponseBytes.ToString()
         );
         processStartInfo.ArgumentList.Add("--output");
         processStartInfo.ArgumentList.Add(bodyPath);

         // A single --write-out: curl does not concatenate multiple -w
         // options, the last one wins. The marker line carries the status
         // fields; the header JSON follows on the remaining output.
         processStartInfo.ArgumentList.Add("--write-out");
         processStartInfo.ArgumentList.Add(
            $"{EndMarker}|%{{http_code}}|%{{content_type}}|" +
            "%{url_effective}\n%{header_json}"
         );
         processStartInfo.ArgumentList.Add(url.ToString());

         process = Process.Start(processStartInfo);

         if(process is null)
         {
            return new CurlRunResult(
               null,
               null,
               [],
               [],
               "Could not start curl process."
            );
         }

         using var cancellationRegistration =
            cancellationToken.Register(
               () =>
               {
                  try
                  {
                     if(!process.HasExited)
                     {
                        process.Kill(entireProcessTree: true);
                     }
                  }
                  catch
                  {
                  }
               }
            );

         var stdoutTask = process.StandardOutput.ReadToEndAsync(
            cancellationToken
         );
         var stderrTask = process.StandardError.ReadToEndAsync(
            cancellationToken
         );

         await process.WaitForExitAsync(cancellationToken);

         var stdout = await stdoutTask;
         var stderr = await stderrTask;

         if(File.Exists(bodyPath) &&
            new FileInfo(bodyPath).Length >
               WebPageFetchDefaults.MaximumResponseBytes)
         {
            return new CurlRunResult(
               null,
               null,
               [],
               [],
               null,
               WebPageFetchErrorKind.ResponseTooLarge
            );
         }

         byte[] body;
         try
         {
            body = File.Exists(bodyPath)
               ? await File.ReadAllBytesAsync(bodyPath, cancellationToken)
               : [];
         }
         catch(Exception exception)
         {
            return new CurlRunResult(
               null,
               null,
               [],
               [],
               $"Could not read curl output: " +
                  WebPageFetchLogging.SummarizeException(exception)
            );
         }

         return ParseCurlOutput(stdout, stderr, process.ExitCode, body);
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(Exception exception)
      {
         return new CurlRunResult(
            null,
            null,
            [],
            [],
            WebPageFetchLogging.SummarizeException(exception)
         );
      }
      finally
      {
         process?.Dispose();

         try
         {
            if(File.Exists(bodyPath))
            {
               File.Delete(bodyPath);
            }
         }
         catch
         {
         }
      }
   }

   private static CurlRunResult ParseCurlOutput(
      string stdout,
      string stderr,
      int exitCode,
      byte[] body
   )
   {
      if(exitCode == CurlFileSizeExceededExitCode)
      {
         return new CurlRunResult(
            null,
            null,
            [],
            body,
            null,
            WebPageFetchErrorKind.ResponseTooLarge
         );
      }

      var markerIndex = stdout.IndexOf(EndMarker, StringComparison.Ordinal);

      if(markerIndex < 0)
      {
         return new CurlRunResult(
            null,
            null,
            [],
            body,
            $"curl exited with code {exitCode}: " +
               Truncate(stderr, 300)
         );
      }

      var afterMarker = stdout[(markerIndex + EndMarker.Length)..]
         .TrimStart('|', ' ', '\r', '\n');
      var newlineIndex = afterMarker.IndexOf('\n');
      var statusLine = (newlineIndex < 0
         ? afterMarker
         : afterMarker[..newlineIndex]).Trim();
      var headerJson = newlineIndex < 0
         ? string.Empty
         : afterMarker[(newlineIndex + 1)..].Trim();

      var fields = statusLine.Split('|');

      if(fields.Length < 2 ||
         !int.TryParse(fields[0], out var statusCode))
      {
         return new CurlRunResult(
            null,
            null,
            [],
            body,
            $"curl returned an unexpected status line " +
               $"(exit code {exitCode})."
         );
      }

      var contentType = fields[1];
      var headers = ParseHeaderJson(headerJson);

      if(exitCode != 0)
      {
         return new CurlRunResult(
            statusCode,
            string.IsNullOrWhiteSpace(contentType)
               ? null
               : contentType,
            headers,
            body,
            $"curl exited with code {exitCode}: " +
               Truncate(stderr, 300)
         );
      }

      return new CurlRunResult(
         statusCode,
         string.IsNullOrWhiteSpace(contentType) ? null : contentType,
         headers,
         body,
         null
      );
   }

   private static IReadOnlyList<KeyValuePair<string, string>>
      ParseHeaderJson(string headerJson)
   {
      if(string.IsNullOrWhiteSpace(headerJson))
      {
         return [];
      }

      try
      {
         using var document = JsonDocument.Parse(headerJson);
         var root = document.RootElement;

         // curl's %{header_json} is a flat object: header name maps to an
         // array of string values. Each value becomes its own entry so a
         // lookup by name can return the last one, matching HTTP semantics.
         if(root.ValueKind != JsonValueKind.Object)
         {
            return [];
         }

         var result = new List<KeyValuePair<string, string>>();
         foreach(var property in root.EnumerateObject())
         {
            if(property.Value.ValueKind != JsonValueKind.Array)
            {
               continue;
            }

            foreach(var value in property.Value.EnumerateArray())
            {
               result.Add(new KeyValuePair<string, string>(
                  property.Name,
                  value.GetString() ?? ""
               ));
            }
         }

         return result.ToArray();
      }
      catch(JsonException)
      {
         // Older curl versions do not support %{header_json}. Redirect
         // handling degrades to a terminal redirect response.
      }

      return [];
   }

   private static string? FindHeaderValue(
      IReadOnlyList<KeyValuePair<string, string>> headers,
      string name
   )
   {
      // Last occurrence wins, matching HTTP header semantics. Returning
      // null (not an empty string) for a missing header matters: a
      // redirect without Location must fail, not follow itself.
      for(var i = headers.Count - 1; i >= 0; i--)
      {
         if(string.Equals(
            headers[i].Key,
            name,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            return headers[i].Value;
         }
      }

      return null;
   }

   private static string Truncate(string value, int maxLength)
   {
      var trimmed = value.Trim();
      return trimmed.Length <= maxLength
         ? trimmed
         : trimmed[..maxLength];
   }

   private sealed record CurlRunResult(
      int? StatusCode,
      string? ContentType,
      IReadOnlyList<KeyValuePair<string, string>> Headers,
      byte[] Body,
      string? TransportError,
      WebPageFetchErrorKind? ErrorKind = null
   );
}
