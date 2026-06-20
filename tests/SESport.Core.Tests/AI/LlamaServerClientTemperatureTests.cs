using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public sealed class LlamaServerClientTemperatureTests
{
   [Fact]
   public void GetEffectiveTemperatureReturnsBaseTemperatureWhenStable()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         0.0m,
         false
      );

      Assert.Equal(0.0m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureBoostsWhenToolCallsRepeat()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         0.0m,
         true
      );

      Assert.Equal(0.15m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureKeepsHigherBaseTemperature()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         0.25m,
         true
      );

      Assert.Equal(0.25m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureReturnsNullWhenBaseIsNull()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         null,
         true
      );

      Assert.Null(temperature);
   }
}
