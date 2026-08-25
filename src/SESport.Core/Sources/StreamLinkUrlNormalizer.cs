namespace SESport.Core.Sources;

public static class StreamLinkUrlNormalizer
{
   private const string DestinationMarker = "destination:";
   private const string UrlParameterName = "url";
   private const string ShortUrlParameterName = "u";
   private const string TrackingQueryPrefix = "utm_";
   private const string TrackingTagParameterName = "tag";

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

      if(markerIndex >= 0)
      {
         var destinationValue = pathAndQuery[
            (markerIndex + DestinationMarker.Length)..
         ];
         destinationValue = Uri.UnescapeDataString(destinationValue);

         if(TryNormalizeNestedUrl(
            destinationValue,
            out normalizedUrl
         ))
         {
            return true;
         }
      }

      foreach(var parameter in parsedSourceUrl.Query
         .TrimStart('?')
         .Split('&', StringSplitOptions.RemoveEmptyEntries))
      {
         var separatorIndex = parameter.IndexOf('=');
         if(separatorIndex <= 0)
         {
            continue;
         }

         var parameterName = Uri.UnescapeDataString(
            parameter[..separatorIndex]
         );
         if(!IsNestedUrlParameter(parameterName))
         {
            continue;
         }

         var nestedUrl = Uri.UnescapeDataString(
            parameter[(separatorIndex + 1)..]
         );
         if(TryNormalizeNestedUrl(nestedUrl, out normalizedUrl))
         {
            return true;
         }
      }

      normalizedUrl = RemoveTrackingQueryParameters(sourceUrl);
      return true;
   }

   private static bool IsNestedUrlParameter(string parameterName)
   {
      return string.Equals(
            parameterName,
            UrlParameterName,
            StringComparison.OrdinalIgnoreCase
         ) ||
         string.Equals(
            parameterName,
            ShortUrlParameterName,
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static bool TryNormalizeNestedUrl(
      string nestedUrl,
      out string normalizedUrl
   )
   {
      if(!SourceUrlNormalizer.TryNormalize(
         nestedUrl,
         out var sourceUrl
      ))
      {
         normalizedUrl = string.Empty;
         return false;
      }

      normalizedUrl = RemoveTrackingQueryParameters(sourceUrl);
      return true;
   }

   private static string RemoveTrackingQueryParameters(string sourceUrl)
   {
      if(!Uri.TryCreate(
         sourceUrl,
         UriKind.Absolute,
         out var parsedUrl
      ) || string.IsNullOrEmpty(parsedUrl.Query))
      {
         return sourceUrl;
      }

      var retainedParameters = parsedUrl.Query
         .TrimStart('?')
         .Split('&', StringSplitOptions.RemoveEmptyEntries)
         .Where(parameter => !IsTrackingQueryParameter(parameter))
         .ToArray();
      var normalizedUrl = parsedUrl.GetLeftPart(UriPartial.Path);

      if(retainedParameters.Length > 0)
      {
         normalizedUrl += "?" + string.Join(
            "&",
            retainedParameters
         );
      }

      return normalizedUrl + parsedUrl.Fragment;
   }

   private static bool IsTrackingQueryParameter(string parameter)
   {
      var separatorIndex = parameter.IndexOf('=');
      var parameterName = separatorIndex >= 0
         ? parameter[..separatorIndex]
         : parameter;
      parameterName = Uri.UnescapeDataString(parameterName);

      return parameterName.StartsWith(
            TrackingQueryPrefix,
            StringComparison.OrdinalIgnoreCase
         ) || string.Equals(
            parameterName,
            TrackingTagParameterName,
            StringComparison.OrdinalIgnoreCase
         );
   }
}
