using System.Globalization;

using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Web.Preferences;

public abstract class DatePreferenceStore(string cookieName)
{
   public DateOnly ResolveDate(HttpContext context, DateOnly? requestedDate)
   {
      var selectedDate = requestedDate ??
         TryReadDate(context.Request) ??
         SportDay.Today(DateTimeOffset.UtcNow).StartDate;

      WriteDateCookie(context, selectedDate);

      return selectedDate;
   }

   public DateOnly? ResolveOptionalDate(
      HttpContext context,
      DateOnly? requestedDate
   )
   {
      var selectedDate = context.Request.Query.ContainsKey(RouteKeys.Date)
         ? requestedDate
         : TryReadDate(context.Request);

      WriteDateCookie(context, selectedDate);
      return selectedDate;
   }

   private void WriteDateCookie(
      HttpContext context,
      DateOnly? selectedDate
   )
   {
      context.Response.Cookies.Append(
         cookieName,
         selectedDate is null
            ? string.Empty
            : DateDisplay.Format(selectedDate.Value),
         new CookieOptions
         {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            IsEssential = true,
            Path = "/Admin",
            SameSite = SameSiteMode.Lax
         }
      );
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
