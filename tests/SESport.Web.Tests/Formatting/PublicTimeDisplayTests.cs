using SESport.Web.Formatting;

namespace SESport.Core.Tests.Formatting;

public sealed class PublicTimeDisplayTests
{
   [Theory]
   [InlineData(null, "")]
   [InlineData("08:14", "08:14")]
   [InlineData("2026-07-26 08:14", "08:14")]
   public void FormatExactTimeTextKeepsTheExactTime(
      string? timeText,
      string expected
   )
   {
      var result = PublicTimeDisplay.FormatExactTimeText(timeText);

      Assert.Equal(expected, result);
   }
}
