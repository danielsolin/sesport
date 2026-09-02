namespace SESport.AI.WebPages;

/// <summary>
/// Structured transport evidence for one HTTP response, shared by the
/// direct HTTP and curl transports.
/// </summary>
internal sealed record WebPageHttpResponse(
   Uri RequestedUrl,
   Uri EffectiveUrl,
   bool Redirected,
   int? StatusCode,
   string? ContentType,
   byte[] Body,
   string? TransportError,
   string? RedirectPolicyError,
   WebPageFetchErrorKind? ErrorKind = null
)
{
   internal bool IsTransportFailure => StatusCode is null;

   internal static WebPageHttpResponse Failure(
      Uri requestedUrl,
      string transportError,
      Uri? effectiveUrl = null
   )
   {
      return new WebPageHttpResponse(
         requestedUrl,
         effectiveUrl ?? requestedUrl,
         effectiveUrl is not null && effectiveUrl != requestedUrl,
         null,
         null,
         [],
         transportError,
         null
      );
   }

   internal static WebPageHttpResponse ResponseTooLarge(
      Uri requestedUrl,
      Uri effectiveUrl,
      int? statusCode,
      string? contentType
   )
   {
      return new WebPageHttpResponse(
         requestedUrl,
         effectiveUrl,
         effectiveUrl != requestedUrl,
         statusCode,
         contentType,
         [],
         null,
         null,
         WebPageFetchErrorKind.ResponseTooLarge
      );
   }

   internal static WebPageHttpResponse RedirectBlocked(
      Uri requestedUrl,
      Uri blockedTarget,
      string policyError
   )
   {
      return new WebPageHttpResponse(
         requestedUrl,
         blockedTarget,
         false,
         null,
         null,
         [],
         null,
         policyError
      );
   }
}
