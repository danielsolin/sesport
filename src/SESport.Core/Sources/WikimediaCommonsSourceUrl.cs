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
   private const string WikiFilePathPrefix = "/wiki/";
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
         !string.IsNullOrEmpty(parsedUrl.Fragment))
      {
         return false;
      }

      string? fileTitle;
      if(string.Equals(
            parsedUrl.AbsolutePath,
            FilePagePath,
            StringComparison.Ordinal
         ))
      {
         if(!TryReadQueryValue(
               parsedUrl.Query,
               "title",
               out fileTitle
            ))
         {
            return false;
         }
      }
      else if(!parsedUrl.AbsolutePath.StartsWith(
               WikiFilePathPrefix,
               StringComparison.Ordinal
            ) ||
            !TryDecode(
               parsedUrl.AbsolutePath[WikiFilePathPrefix.Length..],
               out fileTitle
            ))
      {
         return false;
      }

      if(fileTitle is null ||
         !TryNormalizeFileTitle(fileTitle, out var canonicalTitle) ||
         !TryReadOptionalQueryValue(
            parsedUrl.Query,
            "oldid",
            out var oldid
         ))
      {
         return false;
      }

      var revisionId = 0L;
      if(oldid is not null &&
         (!long.TryParse(
            oldid,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out revisionId
         ) || revisionId <= 0))
      {
         return false;
      }

      reference = CreateReference(canonicalTitle, revisionId);
      return true;
   }

   public static WikimediaCommonsImageReference WithRevision(
      WikimediaCommonsImageReference source,
      long revisionId
   )
   {
      ArgumentNullException.ThrowIfNull(source);
      if(revisionId <= 0)
      {
         throw new ArgumentOutOfRangeException(nameof(revisionId));
      }

      return CreateReference(source.FileTitle, revisionId);
   }

   private static WikimediaCommonsImageReference CreateReference(
      string fileTitle,
      long revisionId
   )
   {
      var encodedTitle = Uri.EscapeDataString(fileTitle)
         .Replace("%3A", ":", StringComparison.OrdinalIgnoreCase);
      var canonicalUrl =
         $"https://{Host}{FilePagePath}?title={encodedTitle}";
      if(revisionId > 0)
      {
         canonicalUrl +=
            $"&oldid={revisionId.ToString(CultureInfo.InvariantCulture)}";
      }

      return new WikimediaCommonsImageReference(
         canonicalUrl,
         fileTitle,
         revisionId
      );
   }

   private static bool TryNormalizeFileTitle(
      string fileTitle,
      out string canonicalTitle
   )
   {
      canonicalTitle = string.Empty;
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

      canonicalTitle = FileTitlePrefix + fileName.Replace(' ', '_');
      return true;
   }

   private static bool TryReadQueryValue(
      string query,
      string key,
      out string value
   )
   {
      if(!TryReadOptionalQueryValue(query, key, out var optionalValue) ||
         optionalValue is null)
      {
         value = string.Empty;
         return false;
      }

      value = optionalValue;
      return true;
   }

   private static bool TryReadOptionalQueryValue(
      string query,
      string key,
      out string? value
   )
   {
      value = null;
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

         if(found ||
            !TryDecode(part[(separator + 1)..], out var decodedValue))
         {
            value = null;
            return false;
         }

         value = decodedValue;
         found = true;
      }

      return true;
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
