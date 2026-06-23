using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SESport.AI.Providers;

internal static class WebPageCurlPageFetcher
{
   internal static async Task<WebPageContent?> FetchAsync(
      ILogger logger,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var output = await RunCurlAsync(
            "curl",
            absoluteUrl,
            cancellationToken
         );

         if(string.IsNullOrWhiteSpace(output))
         {
            return null;
         }

         return ParseCurlOutput(logger, output, absoluteUrl);
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
         return null;
      }
   }

   private static async Task<string> RunCurlAsync(
      string curlPath,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      var processStartInfo = new ProcessStartInfo
      {
         FileName = curlPath,
         RedirectStandardOutput = true,
         RedirectStandardError = true,
         UseShellExecute = false,
         CreateNoWindow = true
      };

      processStartInfo.ArgumentList.Add("--silent");
      processStartInfo.ArgumentList.Add("--show-error");
      processStartInfo.ArgumentList.Add("--location");
      processStartInfo.ArgumentList.Add("--compressed");
      processStartInfo.ArgumentList.Add("--max-time");
      processStartInfo.ArgumentList.Add("30");
      processStartInfo.ArgumentList.Add("--output");
      processStartInfo.ArgumentList.Add("-");
      processStartInfo.ArgumentList.Add("--write-out");
      processStartInfo.ArgumentList.Add(
         "\n__SESPORT_CURL_STATUS__:%{http_code}\n"
      );
      processStartInfo.ArgumentList.Add(absoluteUrl.ToString());

      using var process = Process.Start(processStartInfo);

      if(process is null)
      {
         return string.Empty;
      }

      using var cancellationRegistration = cancellationToken.Register(
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
      _ = await stderrTask;

      return stdout;
   }

   private static WebPageContent? ParseCurlOutput(
      ILogger logger,
      string output,
      Uri absoluteUrl
   )
   {
      var marker = "\n__SESPORT_CURL_STATUS__:";
      var markerIndex = output.LastIndexOf(
         marker,
         StringComparison.Ordinal
      );

      if(markerIndex < 0)
      {
         logger.LogWarning(
            "Curl fallback blocked for {Url} by signature {Signature}.",
            absoluteUrl,
            "<unexpected response>"
         );

         return WebPageContentFetchSupport.BuildFailureContent(
            absoluteUrl,
            null,
            WebPageFetchErrorKind.BrowserBlocked,
            "Curl fallback returned an unexpected response.",
            "curl"
         );
      }

      var body = output[..markerIndex];
      var statusLine = output[(markerIndex + marker.Length)..].Trim();
      var statusCode = statusLine.Split(
         '\n',
         StringSplitOptions.RemoveEmptyEntries
      )[0].Trim();

      if(!string.Equals(statusCode, "200", StringComparison.Ordinal))
      {
         logger.LogWarning(
            "Curl fallback blocked for {Url} by signature {Signature}.",
            absoluteUrl,
            $"HTTP {statusCode}"
         );

         return WebPageContentFetchSupport.BuildFailureContent(
            absoluteUrl,
            null,
            WebPageFetchErrorKind.BrowserBlocked,
            $"Curl fallback returned HTTP {statusCode}.",
            "curl"
         );
      }

      var title = WebPageContentFetchSupport.ExtractHtmlTitle(body);
      var text =
         WebPageContentFetchSupport.ExtractHtmlTextWithEmbeddedState(body);

      if(string.IsNullOrWhiteSpace(text))
      {
         logger.LogWarning(
            "Curl fallback blocked for {Url} by signature {Signature}.",
            absoluteUrl,
            "<no text>"
         );

         return WebPageContentFetchSupport.BuildFailureContent(
            absoluteUrl,
            title,
            WebPageFetchErrorKind.BrowserBlocked,
            "Curl fallback produced no text.",
            "curl"
         );
      }

      if(WebPageBlockDetection.IsBlocked(
         title,
         text,
         WebPageBlockSource.CurlFallback
      ))
      {
         var blockedSignature = WebPageBlockDetection.FindBlockedSignature(
            title,
            text,
            WebPageBlockSource.CurlFallback
         );

         logger.LogWarning(
            "Curl fallback blocked for {Url} by signature {Signature}.",
            absoluteUrl,
            blockedSignature ?? "<unknown>"
         );

         return WebPageContentFetchSupport.BuildFailureContent(
            absoluteUrl,
            title,
            WebPageFetchErrorKind.BrowserBlocked,
            "Curl fallback was blocked.",
            "curl"
         );
      }

      return new WebPageContent(
         title ?? absoluteUrl.ToString(),
         absoluteUrl.ToString(),
         null,
         [],
         WebPageContentFetchSupport.ApplyResponseCutoff(text),
         true,
         text,
         Fetcher: "curl"
      );
   }

}
