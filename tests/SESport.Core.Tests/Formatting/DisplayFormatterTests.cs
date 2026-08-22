using SESport.Core.Formatting;

namespace SESport.Core.Tests.Formatting;

public class DisplayFormatterTests
{
   [Fact]
   public void FormatDateAndTime_ReturnsCombinedText()
   {
      var date = new DateOnly(2026, 7, 15);
      var time = new TimeOnly(13, 30);

      var result = DateDisplay.Format(date, time);

      Assert.Equal("2026-07-15 13:30", result);
   }

   [Fact]
   public void FormatDateAndTime_ReturnsDateWhenTimeIsMissing()
   {
      var date = new DateOnly(2026, 7, 15);

      var result = DateDisplay.Format(date, null);

      Assert.Equal("2026-07-15", result);
   }

   [Theory]
   [InlineData(null, "")]
   [InlineData("", "")]
   [InlineData("13:30", "13:30")]
   [InlineData("2026-07-15 13:30", "13:30")]
   public void FormatTimeOnlyText_ReturnsExpectedText(
      string? timeText,
      string expected
   )
   {
      var result = TimeTextFormatter.FormatTimeOnlyText(timeText);

      Assert.Equal(expected, result);
   }

}
