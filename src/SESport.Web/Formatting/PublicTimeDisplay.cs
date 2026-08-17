using SESport.Core.Domain;
using SESport.Core.Formatting;
using System.Globalization;

namespace SESport.Web.Formatting;

public static class PublicTimeDisplay
{
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
      return string.Concat(
         ApproximationPrefix,
         FormatExactTime(time)
      );
   }

   public static string FormatCurrentMarker(DateTimeOffset now)
   {
      var localNow = TimeZoneHelper.ToLocal(now, SportDay.TimeZoneId);

      return $"Nu {localNow:HH:mm}";
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
