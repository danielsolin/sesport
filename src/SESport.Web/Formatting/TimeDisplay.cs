using System.Globalization;

using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Web.Formatting;

public static class TimeDisplay
{
   public static string FormatLocalDate(DateTimeOffset value)
   {
      var localValue = TimeZoneHelper.ToLocal(value, SportDay.TimeZoneId);

      return localValue.ToString(
         DateDisplay.DateOnlyFormat,
         CultureInfo.InvariantCulture
      );
   }

   public static string FormatLocalTime(DateTimeOffset value)
   {
      var localValue = TimeZoneHelper.ToLocal(value, SportDay.TimeZoneId);

      return localValue.ToString(
         DateDisplay.TimeOnlyFormat,
         CultureInfo.InvariantCulture
      );
   }

   public static string FormatLocalTimeWithoutSeconds(
      DateTimeOffset value
   )
   {
      var localValue = TimeZoneHelper.ToLocal(value, SportDay.TimeZoneId);

      return localValue.ToString(
         DateDisplay.TimeOnlyMinutesFormat,
         CultureInfo.InvariantCulture
      );
   }

   public static string FormatLocalTimestamp(DateTimeOffset value)
   {
      var localValue = TimeZoneHelper.ToLocal(value, SportDay.TimeZoneId);

      return localValue.ToString(
         DateDisplay.DateTimeSecondsFormat,
         CultureInfo.InvariantCulture
      );
   }

   public static string FormatLocalTimestampWithoutSeconds(
      DateTimeOffset value
   )
   {
      var localValue = TimeZoneHelper.ToLocal(value, SportDay.TimeZoneId);

      return localValue.ToString(
         DateDisplay.DateTimeMinutesFormat,
         CultureInfo.InvariantCulture
      );
   }
}
