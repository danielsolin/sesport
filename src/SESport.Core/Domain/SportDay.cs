namespace SESport.Core.Domain;

public sealed record SportDayWindow(
   DateOnly StartDate,
   DateOnly EndDateExclusive,
   TimeOnly Cutoff
);

public static class SportDay
{
   public const string TimeZoneId = "Europe/Stockholm";

   public static readonly TimeOnly Cutoff = new(4, 0);

   public static DateOnly GetLocalDate(DateTimeOffset instant)
   {
      var timeZone = ResolveTimeZone(TimeZoneId);
      var local = TimeZoneInfo.ConvertTime(instant, timeZone);
      return DateOnly.FromDateTime(local.DateTime);
   }

   public static DateOnly GetSportDate(DateTimeOffset instant)
   {
      var timeZone = ResolveTimeZone(TimeZoneId);
      var local = TimeZoneInfo.ConvertTime(instant, timeZone);
      var localDate = DateOnly.FromDateTime(local.DateTime);
      var localTime = TimeOnly.FromDateTime(local.DateTime);

      return localTime < Cutoff ? localDate.AddDays(-1) : localDate;
   }

   public static SportDayWindow Today(DateTimeOffset instant)
   {
      return CreateWindow(instant, 0);
   }

   public static SportDayWindow Tomorrow(DateTimeOffset instant)
   {
      return CreateWindow(instant, 1);
   }

   public static SportDayWindow ForDate(DateOnly date)
   {
      return new SportDayWindow(date, date.AddDays(1), Cutoff);
   }

   public static SportDayWindow CreateWindow(
      DateTimeOffset instant,
      int dayOffset
   )
   {
      var startDate = GetSportDate(instant).AddDays(dayOffset);
      return new SportDayWindow(startDate, startDate.AddDays(1), Cutoff);
   }

   private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
   {
      try
      {
         return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
      }
      catch (TimeZoneNotFoundException)
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
}
