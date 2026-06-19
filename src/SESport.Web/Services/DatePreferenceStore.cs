using System.Globalization;
using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Web.Services;

public abstract class DatePreferenceStore(string cookieName)
{
   public DateOnly ResolveDate(HttpContext context, DateOnly? requestedDate)
   {
      var selectedDate = requestedDate ??
         TryReadDate(context.Request) ??
         SportDay.Today(DateTimeOffset.UtcNow).StartDate;

      context.Response.Cookies.Append(
         cookieName,
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

   private DateOnly? TryReadDate(HttpRequest request)
   {
      if(!request.Cookies.TryGetValue(cookieName, out var value))
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
