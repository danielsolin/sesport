using System.Globalization;

namespace SESport.Core.Formatting;

public static class DateDisplay
{
   public const string DateOnlyFormat = "yyyy-MM-dd";
   public const string MonthDayFormat = "MM-dd";
   public const string TimeOnlyFormat = "HH:mm:ss";
   public const string TimeOnlyMinutesFormat = "HH:mm";
   public const string DateTimeMinutesFormat = "yyyy-MM-dd HH:mm";
   public const string DateTimeSecondsFormat = "yyyy-MM-dd HH:mm:ss";

   public static string Format(DateOnly value)
   {
      return value.ToString(DateOnlyFormat, CultureInfo.InvariantCulture);
   }

   public static string? Format(DateOnly? value)
   {
      return value is null
         ? null
         : Format(value.Value);
   }

   public static string Format(
      DateOnly date,
      TimeOnly? time
   )
   {
      return time is null
         ? Format(date)
         : Format(date) + " " + time.Value.ToString(
            TimeOnlyMinutesFormat,
            CultureInfo.InvariantCulture
         );
   }
}
