namespace SESport.Core.Sources;

public static class SourceUrlNormalizer
{
   public static bool TryNormalize(
      string? sourceUrl,
      out string normalizedUrl
   )
   {
      normalizedUrl = string.Empty;
      var trimmedUrl = sourceUrl?.Trim();

      if(!Uri.TryCreate(
         trimmedUrl,
         UriKind.Absolute,
         out var parsedUrl
      ))
      {
         return false;
      }

      if(parsedUrl.Scheme != Uri.UriSchemeHttp &&
         parsedUrl.Scheme != Uri.UriSchemeHttps)
      {
         return false;
      }

      normalizedUrl = parsedUrl.AbsoluteUri;
      return true;
   }
}
