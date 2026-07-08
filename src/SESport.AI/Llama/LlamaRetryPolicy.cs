using System.Net;

namespace SESport.AI.Llama;

internal static class LlamaRetryPolicy
{
   public static bool IsTransientFailure(
      HttpStatusCode statusCode,
      string rawResponse
   )
   {
      return statusCode == HttpStatusCode.ServiceUnavailable ||
         rawResponse.Contains(
            "Loading model",
            StringComparison.OrdinalIgnoreCase
         );
   }

   public static bool IsTransientFailure(
      Exception exception,
      string rawResponse,
      CancellationToken cancellationToken
   )
   {
      if(exception is HttpRequestException httpRequestException)
      {
         if(httpRequestException.StatusCode is not null)
         {
            return IsTransientFailure(
               httpRequestException.StatusCode.Value,
               rawResponse
            );
         }

         return true;
      }

      if(exception is TaskCanceledException &&
         !cancellationToken.IsCancellationRequested)
      {
         return true;
      }

      if(exception is IOException)
      {
         return true;
      }

      return rawResponse.Contains(
         "Loading model",
         StringComparison.OrdinalIgnoreCase
      );
   }

   public static TimeSpan GetRetryDelay(int attempt)
   {
      var seconds = attempt switch
      {
         1 => 1,
         2 => 2,
         3 => 4,
         4 => 8,
         5 => 16,
         _ => 30
      };

      return TimeSpan.FromSeconds(seconds);
   }

   public static string CreateFailureMessage(
      HttpStatusCode statusCode,
      string rawResponse
   )
   {
      var preview = rawResponse
         .ReplaceLineEndings(" ")
         .Trim();

      if(preview.Length > 240)
      {
         preview = preview[..240] + "...";
      }

      return
         $"llama-server failed with {(int)statusCode} {statusCode}: " +
         preview;
   }
}
