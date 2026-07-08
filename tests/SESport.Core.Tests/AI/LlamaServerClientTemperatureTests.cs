using SESport.AI.Clients;
using SESport.AI.Llama;
using SESport.Core.Domain;

namespace SESport.Core.Tests.AI;

public sealed class LlamaServerClientTemperatureTests
{
   [Fact]
   public void GetEffectiveTemperatureReturnsBaseTemperatureWhenStable()
   {
      var temperature = LlamaTemperature.GetEffectiveTemperature(
         0.0m,
         0
      );

      Assert.Equal(0.0m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureBoostsWhenToolCallsRepeat()
   {
      var temperature = LlamaTemperature.GetEffectiveTemperature(
         0.0m,
         1
      );

      Assert.Equal(0.15m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureKeepsIncreasingForRepeatingTurns()
   {
      var temperature = LlamaTemperature.GetEffectiveTemperature(
         0.0m,
         3
      );

      Assert.Equal(0.25m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureKeepsHigherBaseTemperature()
   {
      var temperature = LlamaTemperature.GetEffectiveTemperature(
         0.25m,
         1
      );

      Assert.Equal(0.25m, temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureReturnsNullWhenBaseIsNull()
   {
      var temperature = LlamaTemperature.GetEffectiveTemperature(
         null,
         1
      );

      Assert.Null(temperature);
   }

   [Fact]
   public void GetEffectiveTemperatureCapsAtReasonableUpperBound()
   {
      var temperature = LlamaTemperature.GetEffectiveTemperature(
         0.0m,
         20
      );

      Assert.Equal(0.6m, temperature);
   }

   [Fact]
   public void CreateRepeatedToolResultMessageIsShort()
   {
      var message = LlamaToolCallHistory.CreateRepeatedToolResultMessage(
         "web_get_page"
      );

      Assert.Equal(
         "Repeated web_get_page call detected. No new information.",
         message
      );
   }

   [Fact]
   public void CreateRepeatedToolReplayMessageIncludesCachedResult()
   {
      var message = LlamaToolCallHistory.CreateRepeatedToolReplayMessage(
         "web_get_page",
         "Page URL: https://example.test/roster"
      );

      Assert.Contains(
         "Repeated web_get_page call detected. No new information.",
         message
      );
      Assert.Contains("Page URL: https://example.test/roster", message);
   }

   [Fact]
   public void SummarizeToolResultCompactsPageContent()
   {
      var summary = LlamaConversationTrimmer.SummarizeToolResult(
         WebToolNames.GetPage,
         """
         Page URL: https://example.test/roster
         Title: Huge Article
         URL: https://example.test/roster
         Page text:
         KEEP-ME-ROUND-2-KEEP-ME-ROUND-2-
         """
      );

      Assert.NotEmpty(summary);
      Assert.DoesNotContain("KEEP-ME-ROUND-2-", summary);
   }
}
