using System.Text.Json;
using System.Text.RegularExpressions;

namespace SESport.Data.AI;

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

         if(!root.TryGetProperty("Sources", out var sources) &&
            !root.TryGetProperty("sources", out sources))
         {
            return [];
         }

         if(sources.ValueKind != JsonValueKind.Array)
         {
            return [];
         }

         foreach(var item in sources.EnumerateArray())
         {
            if(item.ValueKind != JsonValueKind.String)
            {
               continue;
            }

            var candidate = item.GetString();

            if(string.IsNullOrWhiteSpace(candidate))
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
      catch(JsonException)
      {
         return [];
      }
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
