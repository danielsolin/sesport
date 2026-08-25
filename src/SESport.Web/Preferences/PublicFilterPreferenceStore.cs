using SESport.Core.Formatting;

namespace SESport.Web.Preferences;

public static class PublicFilterPreferenceStore
{
   public const string ScheduleCookieName =
      "sesport.public.schedule";
   public const string WatchedCookieName =
      "sesport.public.watched";
   public const string StatisticsCookieName =
      "sesport.public.statistics";

   public static string? ReadScheduleUrl(HttpRequest request)
   {
      return ReadScopedUrl(
         request,
         ScheduleCookieName,
         PublicRoutePaths.Home
      );
   }

   public static string? ReadWatchedUrl(HttpRequest request)
   {
      return ReadScopedUrl(
         request,
         WatchedCookieName,
         PublicRoutePaths.Watched
      );
   }

   public static string? ReadStatisticsUrl(HttpRequest request)
   {
      return ReadScopedUrl(
         request,
         StatisticsCookieName,
         PublicRoutePaths.Statistics
      );
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
      var url = path + queryString;
      SaveUrl(
         response,
         watched ? WatchedCookieName : ScheduleCookieName,
         url
      );
   }

   public static void SaveStatistics(
      HttpResponse response,
      string selectedMonth,
      string? sport
   )
   {
      var queryString = "?month=" + Uri.EscapeDataString(
         selectedMonth.Trim()
      );
      var normalizedSport = sport?.Trim();
      if(!string.IsNullOrWhiteSpace(normalizedSport))
      {
         queryString += "&sport=" + Uri.EscapeDataString(
            normalizedSport
         );
      }

      SaveUrl(
         response,
         StatisticsCookieName,
         PublicRoutePaths.Statistics + queryString
      );
   }

   private static string? ReadScopedUrl(
      HttpRequest request,
      string cookieName,
      string expectedPath
   )
   {
      if(!request.Cookies.TryGetValue(cookieName, out var value))
      {
         return null;
      }

      return IsAllowedUrl(value, expectedPath) ? value : null;
   }

   private static bool IsAllowedUrl(
      string? value,
      string expectedPath
   )
   {
      return !string.IsNullOrWhiteSpace(value) &&
         value.Length <= 256 &&
         value.StartsWith("/", StringComparison.Ordinal) &&
         string.Equals(
            GetPath(value),
            expectedPath,
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static string GetPath(string value)
   {
      var queryStart = value.IndexOf('?', StringComparison.Ordinal);
      return queryStart >= 0 ? value[..queryStart] : value;
   }

   private static void SaveUrl(
      HttpResponse response,
      string cookieName,
      string url
   )
   {
      response.Cookies.Append(
         cookieName,
         url,
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
