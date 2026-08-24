namespace SESport.Core.Sources;

public static class StreamLinkUrlNormalizer
{
   private const string DestinationMarker = "destination:";

   public static bool TryNormalize(
      string? streamUrl,
      out string normalizedUrl
   )
   {
      normalizedUrl = string.Empty;

      if(!SourceUrlNormalizer.TryNormalize(
         streamUrl,
         out var sourceUrl
      ) || !Uri.TryCreate(
         sourceUrl,
         UriKind.Absolute,
         out var parsedSourceUrl
      ))
      {
         return false;
      }

      var pathAndQuery = parsedSourceUrl.PathAndQuery;
      var markerIndex = pathAndQuery.IndexOf(
         DestinationMarker,
         StringComparison.OrdinalIgnoreCase
      );

      if(markerIndex < 0)
      {
         normalizedUrl = sourceUrl;
         return true;
      }

      var destinationValue = pathAndQuery[
         (markerIndex + DestinationMarker.Length)..
      ];
      destinationValue = Uri.UnescapeDataString(destinationValue);

      if(!SourceUrlNormalizer.TryNormalize(
         destinationValue,
         out var destinationUrl
      ))
      {
         normalizedUrl = sourceUrl;
         return true;
      }

      normalizedUrl = destinationUrl;
      return true;
   }
}
