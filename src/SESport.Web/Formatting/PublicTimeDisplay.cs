using System.Globalization;

using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Web.Formatting;

public static class PublicTimeDisplay
{
   private const int MinutesPerHalfHour = 30;
   private const int SecondsPerMinute = 60;
   private const int SecondsPerHalfHour =
      MinutesPerHalfHour * SecondsPerMinute;
   private const int RoundingOffsetSeconds = SecondsPerHalfHour / 2;
   private const string ApproximationPrefix = "≈";

   public static string FormatExactTimeText(
      string? timeText,
      TimeOnly? localTime = null
   )
   {
      return localTime?.ToString(
         DateDisplay.TimeOnlyMinutesFormat,
         CultureInfo.InvariantCulture
      ) ?? TimeTextFormatter.FormatTimeOnlyText(timeText);
   }

   public static string FormatApproximateTimeText(
      string? timeText,
      TimeOnly? localTime = null
   )
   {
      var time = localTime ?? ParseTime(timeText);

      return time is null
         ? FormatExactTimeText(timeText)
         : FormatApproximateTime(time.Value);
   }

   public static string? FormatExactTime(TimeOnly? time)
   {
      return time?.ToString(
         DateDisplay.TimeOnlyMinutesFormat,
         CultureInfo.InvariantCulture
      );
   }

   public static string? FormatApproximateTime(TimeOnly? time)
   {
      return time is null ? null : FormatApproximateTime(time.Value);
   }

   public static string FormatApproximateTime(TimeOnly time)
   {
      var roundedTime = RoundToNearestHalfHour(time);

      return string.Concat(
         ApproximationPrefix,
         FormatExactTime(roundedTime)
      );
   }

   public static string FormatCurrentMarker(DateTimeOffset now)
   {
      var localNow = TimeZoneHelper.ToLocal(now, SportDay.TimeZoneId);

      return $"Nu {localNow:HH:mm}";
   }

   public static TimeOnly RoundToNearestHalfHour(TimeOnly time)
   {
      var totalSeconds = (int)time.ToTimeSpan().TotalSeconds;
      var roundedSeconds =
         (totalSeconds + RoundingOffsetSeconds) /
         SecondsPerHalfHour *
         SecondsPerHalfHour;
      var minutesInDay = 24 * 60;
      var roundedMinutes =
         roundedSeconds / SecondsPerMinute % minutesInDay;

      return new TimeOnly(roundedMinutes / 60, roundedMinutes % 60);
   }

   private static TimeOnly? ParseTime(string? timeText)
   {
      var timeOnlyText = TimeTextFormatter.FormatTimeOnlyText(timeText);

      return TimeOnly.TryParseExact(
         timeOnlyText,
         [
            DateDisplay.TimeOnlyMinutesFormat,
            DateDisplay.TimeOnlyFormat
         ],
         CultureInfo.InvariantCulture,
         DateTimeStyles.None,
         out var time
      )
         ? time
         : null;
   }
}
