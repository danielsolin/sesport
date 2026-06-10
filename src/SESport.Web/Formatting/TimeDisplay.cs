using SESport.Core.Formatting;

namespace SESport.Web.Formatting;

public static class TimeDisplay
{
   private const string TimeZoneId = "Europe/Stockholm";

   public static string FormatLocalTimestamp(DateTimeOffset value)
   {
      var localValue = TimeZoneHelper.ToLocal(value, TimeZoneId);

      return localValue.ToString("yyyy-MM-dd HH:mm:ss");
   }
}
