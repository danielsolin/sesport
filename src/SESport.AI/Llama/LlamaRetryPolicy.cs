using System.Net;

using SESport.Core.Configuration;

namespace SESport.AI.Llama;

internal static class LlamaRetryPolicy
{
   public static bool IsTransientFailure(
      HttpStatusCode statusCode,
      string rawResponse
   )
   {
      if(IsStructuredOutputFormatFailure(rawResponse))
      {
         return false;
      }

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
      if(IsStructuredOutputFormatFailure(rawResponse) ||
         IsStructuredOutputFormatFailure(exception.Message))
      {
         return false;
      }

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

   private static bool IsStructuredOutputFormatFailure(string value)
   {
      return value.Contains(
         "peg-native format",
         StringComparison.OrdinalIgnoreCase
      );
   }

   public static TimeSpan GetRetryDelay(int attempt)
   {
      var retryDelays = LlamaServerDefaults.TransientRetryDelays;

      if(attempt < 1 || attempt > retryDelays.Count)
      {
         return retryDelays[^1];
      }

      return retryDelays[attempt - 1];
   }

   public static string CreateFailureMessage(
      HttpStatusCode statusCode,
      string rawResponse
   )
   {
      var preview = rawResponse
         .ReplaceLineEndings(" ")
         .Trim();

      if(preview.Length > LlamaServerDefaults.PreviewSnippetCharacters)
      {
         preview = preview[..LlamaServerDefaults.PreviewSnippetCharacters] +
            "...";
      }

      return
         $"llama-server failed with {(int)statusCode} {statusCode}: " +
         preview;
   }
}
