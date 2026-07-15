using SESport.AI.Llama;

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
   public void CreateRepeatedToolResultMessageRedirectsResearch()
   {
      var message = LlamaToolCallHistory.CreateRepeatedToolResultMessage(
         "web_get_page"
      );

      Assert.Contains("Repeated web_get_page call detected.", message);
      Assert.Contains("consumed research budget", message);
      Assert.Contains("different query, URL, or find value", message);
   }

   [Fact]
   public void CreateRepeatedToolReplayMessageIncludesCachedResult()
   {
      var message = LlamaToolCallHistory.CreateRepeatedToolReplayMessage(
         "web_get_page",
         "Page URL: https://example.test/roster"
      );

      Assert.Contains(
         "Repeated web_get_page call detected.",
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
