using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Web.Formatting;

public static class TimeDisplay
{
   public static string FormatLocalDate(DateTimeOffset value)
   {
      var localValue = TimeZoneHelper.ToLocal(value, SportDay.TimeZoneId);

      return localValue.ToString("yyyy-MM-dd");
   }

   public static string FormatLocalTime(DateTimeOffset value)
   {
      var localValue = TimeZoneHelper.ToLocal(value, SportDay.TimeZoneId);

      return localValue.ToString("HH:mm:ss");
   }

   public static string FormatLocalTimestamp(DateTimeOffset value)
   {
      var localValue = TimeZoneHelper.ToLocal(value, SportDay.TimeZoneId);

      return localValue.ToString("yyyy-MM-dd HH:mm:ss");
   }
}
