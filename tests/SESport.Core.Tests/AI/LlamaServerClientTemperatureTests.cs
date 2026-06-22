using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public sealed class LlamaServerClientTemperatureTests
{
   [Fact]
   public void GetEffectiveTemperatureReturnsBaseTemperatureWhenStable()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         0.0m,
         0
      );

      Assert.Equal(0.0m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureBoostsWhenToolCallsRepeat()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         0.0m,
         1
      );

      Assert.Equal(0.15m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureKeepsIncreasingForRepeatingTurns()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         0.0m,
         3
      );

      Assert.Equal(0.25m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureKeepsHigherBaseTemperature()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         0.25m,
         1
      );

      Assert.Equal(0.25m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureReturnsNullWhenBaseIsNull()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         null,
         1
      );

      Assert.Null(temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureCapsAtReasonableUpperBound()
   {
      var temperature = LlamaServerClient.GetEffectiveTemperature(
         0.0m,
         20
      );

      Assert.Equal(0.6m, temperature);
   }
}
