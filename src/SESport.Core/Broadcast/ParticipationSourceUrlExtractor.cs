using System.Text.Json;
using System.Text.RegularExpressions;

namespace SESport.Core.Broadcast;

public static class ParticipationSourceUrlExtractor
{
   private static readonly Regex UrlPattern = new(
      @"https?://[^\s""'<>()\[\]{}]+",
      RegexOptions.Compiled |
      RegexOptions.IgnoreCase |
      RegexOptions.CultureInvariant
   );

   public static IReadOnlyList<string> Extract(string? rawResponseJson)
   {
      if(string.IsNullOrWhiteSpace(rawResponseJson))
      {
         return [];
      }

      var urls = new List<string>();
      var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach(Match match in UrlPattern.Matches(rawResponseJson))
      {
         var candidate = Normalize(match.Value);

         if(candidate == string.Empty)
         {
            continue;
         }

         if(!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
         {
            continue;
         }

         if(!string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttp,
            StringComparison.OrdinalIgnoreCase
         ) &&
         !string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            continue;
         }

         var url = uri.AbsoluteUri;

         if(seen.Add(url))
         {
            urls.Add(url);
         }
      }

      return urls;
   }

   public static IReadOnlyList<string> ExtractFromOutput(string? outputText)
   {
      if(string.IsNullOrWhiteSpace(outputText))
      {
         return [];
      }

      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.ValueKind != JsonValueKind.Object)
         {
            return [];
         }

         var urls = new List<string>();
         var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

         AddSourceUrls(root, "CheckedSources", urls, seen);

         return urls;
      }
      catch(JsonException)
      {
         return [];
      }
   }

   private static void AddSourceUrls(
      JsonElement root,
      string propertyName,
      List<string> urls,
      HashSet<string> seen
   )
   {
      if(!root.TryGetProperty(propertyName, out var sources) &&
         !root.TryGetProperty(
            char.ToLowerInvariant(propertyName[0]) + propertyName[1..],
            out sources
         ))
      {
         return;
      }

      if(sources.ValueKind != JsonValueKind.Array)
      {
         return;
      }

      foreach(var item in sources.EnumerateArray())
      {
         var candidate = ReadSourceUrl(item);

         if(string.IsNullOrWhiteSpace(candidate))
         {
            continue;
         }

         candidate = Normalize(candidate);

         if(!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            !IsHttpUrl(uri))
         {
            continue;
         }

         var url = uri.AbsoluteUri;

         if(seen.Add(url))
         {
            urls.Add(url);
         }
      }
   }

   private static string? ReadSourceUrl(JsonElement item)
   {
      if(item.ValueKind == JsonValueKind.String)
      {
         return item.GetString();
      }

      if(item.ValueKind == JsonValueKind.Object &&
         item.TryGetProperty("Url", out var url) &&
         url.ValueKind == JsonValueKind.String)
      {
         return url.GetString();
      }

      return null;
   }

   private static bool IsHttpUrl(Uri uri)
   {
      return string.Equals(
         uri.Scheme,
         Uri.UriSchemeHttp,
         StringComparison.OrdinalIgnoreCase
      ) || string.Equals(
         uri.Scheme,
         Uri.UriSchemeHttps,
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static string Normalize(string value)
   {
      return value.Trim().TrimEnd(
         '.',
         ',',
         ';',
         ':',
         '!',
         '?',
         ')',
         ']',
         '}',
         '"',
         '\''
      );
   }
}
