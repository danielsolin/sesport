using SESport.Core.Configuration;

namespace SESport.Core.Tests.Configuration;

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

   [Fact]
   public void DefaultRateLimitedCooldownIsFifteenMinutes()
   {
      var options = new WebSearchRateLimitOptions();

      Assert.Equal(
         TimeSpan.FromMinutes(15),
         options.RateLimitedCooldown
      );
   }
}
