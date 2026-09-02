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
   string? RedirectPolicyError
)
{
   internal bool IsTransportFailure => StatusCode is null;

   internal static WebPageHttpResponse Failure(
      Uri requestedUrl,
      string transportError
   )
   {
      return new WebPageHttpResponse(
         requestedUrl,
         requestedUrl,
         false,
         null,
         null,
         [],
         transportError,
         null
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
