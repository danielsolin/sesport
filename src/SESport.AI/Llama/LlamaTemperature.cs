using System.Text.Json.Nodes;

namespace SESport.AI.Llama;

internal static class LlamaTemperature
{
   public static decimal? GetRequestTemperature(JsonObject request)
   {
      if(!request.TryGetPropertyValue("temperature", out var value))
      {
         return null;
      }

      return value is JsonValue jsonValue &&
         jsonValue.TryGetValue<decimal>(out var temperature)
         ? temperature
         : null;
   }

   public static decimal? GetEffectiveTemperature(
      decimal? baseTemperature,
      int repeatedToolCallStreak
   )
   {
      if(baseTemperature is null)
      {
         return null;
      }

      if(repeatedToolCallStreak <= 0)
      {
         return baseTemperature;
      }

      var adjustedTemperature = 0.15m + (repeatedToolCallStreak - 1) * 0.05m;
      adjustedTemperature = Math.Min(adjustedTemperature, 0.6m);

      return Math.Max(baseTemperature.Value, adjustedTemperature);
   }

   public static void ApplyTemperature(
      JsonObject request,
      decimal? baseTemperature,
      int repeatedToolCallStreak
   )
   {
      var effectiveTemperature = GetEffectiveTemperature(
         baseTemperature,
         repeatedToolCallStreak
      );

      if(effectiveTemperature is null)
      {
         return;
      }

      request["temperature"] = effectiveTemperature.Value;
   }
}
