using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Web.Formatting;

public static class TimeDisplay
{
   public static string FormatLocalTimestamp(DateTimeOffset value)
   {
      var localValue = TimeZoneHelper.ToLocal(value, SportDay.TimeZoneId);

      return localValue.ToString("yyyy-MM-dd HH:mm:ss");
   }
}
