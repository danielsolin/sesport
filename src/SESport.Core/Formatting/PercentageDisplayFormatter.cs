using System.Globalization;

namespace SESport.Core.Formatting;

public static class PercentageDisplayFormatter
{
   public static string FormatWholePercent(decimal? value)
   {
      return value is null
         ? string.Empty
         : Math.Floor(value.Value * 100)
            .ToString(CultureInfo.InvariantCulture);
   }
}
