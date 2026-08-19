using SESport.Core.Formatting;

namespace SESport.Web.Preferences;

public static class PublicFilterPreferenceStore
{
   public const string CookieName = "sesport.public.filters";

   public static string? ReadQueryString(HttpRequest request)
   {
      if(!request.Cookies.TryGetValue(CookieName, out var value)
         || string.IsNullOrWhiteSpace(value)
         || value.Length > 256
         || !value.StartsWith("?", StringComparison.Ordinal))
      {
         return null;
      }

      return value;
   }

   public static void Save(
      HttpResponse response,
      DateOnly selectedDate,
      string? sport
   )
   {
      var queryString = "?date=" + DateDisplay.Format(selectedDate);
      var normalizedSport = sport?.Trim();
      if(!string.IsNullOrWhiteSpace(normalizedSport))
      {
         queryString += "&sport=" +
            Uri.EscapeDataString(normalizedSport);
      }

      response.Cookies.Append(
         CookieName,
         queryString,
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
