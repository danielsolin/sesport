namespace SESport.Core.Formatting;

public static class TimeZoneHelper
{
   public static TimeZoneInfo Resolve(string timeZoneId)
   {
      try
      {
         return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
      }
      catch(TimeZoneNotFoundException)
      {
         if(OperatingSystem.IsWindows() &&
            TimeZoneInfo.TryConvertIanaIdToWindowsId(
               timeZoneId,
               out var windowsTimeZoneId
            ))
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

   public static DateTimeOffset ToUtc(
      DateOnly date,
      TimeOnly time,
      string timeZoneId
   )
   {
      var local = date.ToDateTime(time, DateTimeKind.Unspecified);
      var timeZone = Resolve(timeZoneId);
      var offset = timeZone.GetUtcOffset(local);

      return new DateTimeOffset(local, offset).ToUniversalTime();
   }

   public static DateTimeOffset ToLocal(
      DateTimeOffset value,
      string timeZoneId
   )
   {
      return TimeZoneInfo.ConvertTime(value, Resolve(timeZoneId));
   }
}
