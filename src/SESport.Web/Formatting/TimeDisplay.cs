namespace SESport.Web.Formatting;

public static class TimeDisplay
{
   private const string TimeZoneId = "Europe/Stockholm";

   public static string FormatLocalTimestamp(DateTimeOffset value)
   {
      var timeZone = ResolveTimeZone();
      var localValue = TimeZoneInfo.ConvertTime(value, timeZone);

      return localValue.ToString("yyyy-MM-dd HH:mm:ss");
   }

   private static TimeZoneInfo ResolveTimeZone()
   {
      try
      {
         return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
      }
      catch(TimeZoneNotFoundException)
      {
         if(
            OperatingSystem.IsWindows() &&
            TimeZoneInfo.TryConvertIanaIdToWindowsId(
               TimeZoneId,
               out var windowsTimeZoneId
            )
         )
         {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
         }

         return TimeZoneInfo.Utc;
      }
      catch(InvalidTimeZoneException)
      {
         return TimeZoneInfo.Utc;
      }
   }
}
