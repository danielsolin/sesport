using System.Globalization;

namespace SESport.Core.Sources;

public sealed record WikimediaCommonsImageReference(
   string Url,
   string FileTitle,
   long RevisionId
);

public static class WikimediaCommonsSourceUrl
{
   private const string Host = "commons.wikimedia.org";
   private const string FilePagePath = "/w/index.php";
   private const string FileTitlePrefix = "File:";

   public static bool TryParse(
      string? sourceUrl,
      out WikimediaCommonsImageReference reference
   )
   {
      reference = null!;
      if(!Uri.TryCreate(
            sourceUrl?.Trim(),
            UriKind.Absolute,
            out var parsedUrl
         ) ||
         parsedUrl.Scheme != Uri.UriSchemeHttps ||
         !string.Equals(
            parsedUrl.Host,
            Host,
            StringComparison.OrdinalIgnoreCase
         ) ||
         !string.IsNullOrEmpty(parsedUrl.UserInfo) ||
         (parsedUrl.Port != -1 && parsedUrl.Port != 443) ||
         !string.Equals(
            parsedUrl.AbsolutePath,
            FilePagePath,
            StringComparison.Ordinal
         ) ||
         !string.IsNullOrEmpty(parsedUrl.Fragment) ||
         !TryReadQueryValue(parsedUrl.Query, "title", out var fileTitle) ||
         !TryReadQueryValue(parsedUrl.Query, "oldid", out var oldid) ||
         !long.TryParse(
            oldid,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var revisionId
         ) ||
         revisionId <= 0)
      {
         return false;
      }

      if(!fileTitle.StartsWith(
            FileTitlePrefix,
            StringComparison.OrdinalIgnoreCase
         ))
      {
         return false;
      }

      var fileName = fileTitle[FileTitlePrefix.Length..].Trim();
      if(string.IsNullOrWhiteSpace(fileName))
      {
         return false;
      }

      var canonicalTitle = FileTitlePrefix + fileName.Replace(' ', '_');
      var encodedTitle = Uri.EscapeDataString(canonicalTitle)
         .Replace("%3A", ":", StringComparison.OrdinalIgnoreCase);
      var canonicalUrl =
         $"https://{Host}{FilePagePath}?title={encodedTitle}" +
         $"&oldid={revisionId.ToString(CultureInfo.InvariantCulture)}";

      reference = new WikimediaCommonsImageReference(
         canonicalUrl,
         canonicalTitle,
         revisionId
      );
      return true;
   }

   private static bool TryReadQueryValue(
      string query,
      string key,
      out string value
   )
   {
      value = string.Empty;
      var found = false;

      foreach(var part in query.TrimStart('?').Split(
         '&',
         StringSplitOptions.RemoveEmptyEntries
      ))
      {
         var separator = part.IndexOf('=');
         if(separator <= 0)
         {
            continue;
         }

         if(!TryDecode(part[..separator], out var partKey) ||
            !string.Equals(partKey, key, StringComparison.Ordinal))
         {
            continue;
         }

         if(found || !TryDecode(part[(separator + 1)..], out value))
         {
            value = string.Empty;
            return false;
         }

         found = true;
      }

      return found;
   }

   private static bool TryDecode(string value, out string decoded)
   {
      try
      {
         decoded = Uri.UnescapeDataString(
            value.Replace('+', ' ')
         );
         return true;
      }
      catch(UriFormatException)
      {
         decoded = string.Empty;
         return false;
      }
   }
}
