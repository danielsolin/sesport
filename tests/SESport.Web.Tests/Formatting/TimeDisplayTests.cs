using SESport.Web.Formatting;

namespace SESport.Core.Tests.Formatting;

public sealed class TimeDisplayTests
{
   [Fact]
   public void FormatLocalTimestampWithoutSecondsUsesLocalTime()
   {
      var value = new DateTimeOffset(
         2026,
         8,
         23,
         15,
         15,
         0,
         TimeSpan.Zero
      );

      var result = TimeDisplay.FormatLocalTimestampWithoutSeconds(value);

      Assert.Equal("2026-08-23 17:15", result);
   }
}
