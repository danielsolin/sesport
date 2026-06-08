using System.Globalization;
using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Web.Services;

public sealed class AdminDatePreferenceStore
{
   private const string CookieName = "sesport.admin.date";

   public DateOnly ResolveDate(HttpContext context, DateOnly? requestedDate)
   {
      var selectedDate = requestedDate ??
         TryReadDate(context.Request) ??
         SportDay.Today(DateTimeOffset.UtcNow).StartDate;

      context.Response.Cookies.Append(
         CookieName,
         DateDisplay.Format(selectedDate),
         new CookieOptions
         {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            IsEssential = true,
            Path = "/Admin",
            SameSite = SameSiteMode.Lax
         }
      );

      return selectedDate;
   }

   private static DateOnly? TryReadDate(HttpRequest request)
   {
      if(!request.Cookies.TryGetValue(CookieName, out var value))
      {
         return null;
      }

      return DateOnly.TryParseExact(
         value,
         DateDisplay.DateOnlyFormat,
         CultureInfo.InvariantCulture,
         DateTimeStyles.None,
         out var date
      )
         ? date
         : null;
   }
}
