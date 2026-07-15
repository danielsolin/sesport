using SESport.AI.WebSearch;

namespace SESport.Core.Tests.AI;

public sealed class WebSearchRateLimitOptionsTests
{
   [Fact]
   public void DefaultMinimumRequestIntervalIsTenSeconds()
   {
      var options = new WebSearchRateLimitOptions();

      Assert.Equal(
         TimeSpan.FromSeconds(10),
         options.MinimumRequestInterval
      );
   }
}
