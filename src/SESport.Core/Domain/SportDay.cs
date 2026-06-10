using SESport.Core.Formatting;

namespace SESport.Core.Domain;

public sealed record SportDayWindow(
   DateOnly StartDate,
   DateOnly EndDateExclusive,
   TimeOnly Cutoff
);

public static class SportDay
{
   public const string TimeZoneId = "Europe/Stockholm";

   public static readonly TimeOnly Cutoff = new(4, 1);

   public static DateOnly GetLocalDate(DateTimeOffset instant)
   {
      var local = TimeZoneInfo.ConvertTime(
         instant,
         TimeZoneHelper.Resolve(TimeZoneId)
      );
      return DateOnly.FromDateTime(local.DateTime);
   }

   public static DateOnly GetSportDate(DateTimeOffset instant)
   {
      var local = TimeZoneInfo.ConvertTime(
         instant,
         TimeZoneHelper.Resolve(TimeZoneId)
      );
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
}
