using Microsoft.AspNetCore.Http;

namespace SESport.Web.Preferences;

public sealed class FilterPreferenceStore
{
   public IReadOnlyList<string> ResolveList(
      HttpContext context,
      string queryKey,
      IReadOnlyCollection<string> requestedValues,
      string cookieName
   )
   {
      if(context.Request.Query.ContainsKey(queryKey))
      {
         return requestedValues.ToArray();
      }

      if(!context.Request.Cookies.TryGetValue(cookieName, out var value))
      {
         return [];
      }

      return SplitValues(value);
   }

   public string? ResolveValue(
      HttpContext context,
      string queryKey,
      string? requestedValue,
      string cookieName
   )
   {
      if(context.Request.Query.ContainsKey(queryKey))
      {
         return requestedValue;
      }

      return context.Request.Cookies.TryGetValue(cookieName, out var value)
         ? value?.Trim()
         : requestedValue;
   }

   public bool ResolveBoolean(
      HttpContext context,
      string queryKey,
      bool requestedValue,
      string cookieName
   )
   {
      if(context.Request.Query.ContainsKey(queryKey))
      {
         return context.Request.Query[queryKey].Any(value =>
            bool.TryParse(value, out var parsed) && parsed
         );
      }

      return context.Request.Cookies.TryGetValue(cookieName, out var value) &&
         bool.TryParse(value, out var parsedCookieValue)
            ? parsedCookieValue
            : requestedValue;
   }

   public void WriteList(
      HttpContext context,
      string cookieName,
      IEnumerable<string> values
   )
   {
      var normalizedValues = values
         .Where(value => !string.IsNullOrWhiteSpace(value))
         .Select(value => value.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase);

      WriteCookie(context, cookieName, string.Join("|", normalizedValues));
   }

   public void WriteValue(
      HttpContext context,
      string cookieName,
      string? value
   )
   {
      WriteCookie(context, cookieName, value?.Trim() ?? string.Empty);
   }

   public void WriteBoolean(
      HttpContext context,
      string cookieName,
      bool value
   )
   {
      WriteCookie(context, cookieName, value.ToString());
   }

   private static IReadOnlyList<string> SplitValues(string value)
   {
      return value.Split(
         '|',
         StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries
      );
   }

   private static void WriteCookie(
      HttpContext context,
      string cookieName,
      string value
   )
   {
      context.Response.Cookies.Append(
         cookieName,
         value,
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
}
