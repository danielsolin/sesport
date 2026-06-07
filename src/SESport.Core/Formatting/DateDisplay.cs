using System.Globalization;

namespace SESport.Core.Formatting;

public static class DateDisplay
{
   public const string DateOnlyFormat = "yyyy-MM-dd";

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
}
