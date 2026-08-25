using SESport.Core.Formatting;

namespace SESport.Web.Preferences;

public static class PublicFilterPreferenceStore
{
   public const string CookieName = "sesport.public.filters";

   public static string? ReadPublicActivityUrl(HttpRequest request)
   {
      if(!request.Cookies.TryGetValue(CookieName, out var value)
         || string.IsNullOrWhiteSpace(value)
         || value.Length > 256
         || !value.StartsWith("/", StringComparison.Ordinal))
      {
         return null;
      }

      var queryStart = value.IndexOf('?', StringComparison.Ordinal);
      var path = queryStart >= 0 ? value[..queryStart] : value;
      return path == PublicRoutePaths.Home ||
         string.Equals(
            path,
            PublicRoutePaths.Watched,
            StringComparison.OrdinalIgnoreCase
         )
         ? value
         : null;
   }

   public static void Save(
      HttpResponse response,
      DateOnly selectedDate,
      string? sport,
      bool watched
   )
   {
      var queryString = watched
         ? string.Empty
         : "?date=" + DateDisplay.Format(selectedDate);
      var normalizedSport = sport?.Trim();
      if(!string.IsNullOrWhiteSpace(normalizedSport))
      {
         queryString += queryString.Length == 0 ? "?sport=" : "&sport=";
         queryString +=
            Uri.EscapeDataString(normalizedSport);
      }

      var path = watched
         ? PublicRoutePaths.Watched
         : PublicRoutePaths.Home;
      response.Cookies.Append(
         CookieName,
         path + queryString,
         new CookieOptions
         {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            IsEssential = true,
            Path = "/",
            SameSite = SameSiteMode.Lax
         }
      );
   }
}
