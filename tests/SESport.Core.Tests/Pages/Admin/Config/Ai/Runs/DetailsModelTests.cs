using SESport.Web.Pages.Admin.Config.Ai.Runs;

namespace SESport.Core.Tests.Pages.Admin.Config.Ai.Runs;

public sealed class DetailsModelTests
{
   [Fact]
   public void FormatToolCallReturnsCompactFindSignature()
   {
      var toolCall = new DetailsModel.ToolTraceCallViewModel(
         "call_1",
         WebToolNames.FindInPage,
         """
         {
           "id": "s2_8",
           "find": "Sweden"
         }
         """
      );

      Assert.Equal(
         $"{WebToolNames.FindInPage}('s2_8','Sweden')",
         DetailsModel.FormatToolCall(toolCall)
      );
   }

   [Fact]
   public void FormatToolCallReturnsCompactSearchSignature()
   {
      var toolCall = new DetailsModel.ToolTraceCallViewModel(
         "call_1",
         WebToolNames.Search,
         """
         {
           "query": "Belgien runt Etapp 2 participants",
           "limit": 5
         }
         """
      );

      Assert.Equal(
         $"{WebToolNames.Search}('Belgien runt Etapp 2 participants',5)",
         DetailsModel.FormatToolCall(toolCall)
      );
   }

   [Fact]
   public void GetMaxConversationCharacterCountUsesRoundPeak()
   {
      var run = new SESport.AI.Models.AiRunDetail(
         Id: Guid.NewGuid(),
         JobId: "job",
         JobLabel: "Job",
         PromptId: Guid.NewGuid(),
         PromptVersion: 1,
         SystemPrompt: "System",
         UserPromptTemplate: "User",
         ProviderId: "provider",
         ProviderLabel: "Provider",
         ProviderModel: "Model",
         StatusId: "completed",
         CorrelationId: null,
         InputPayloadJson: "{}",
         RenderedPrompt: "Rendered",
         RawRequestJson: null,
         RawResponseJson: null,
         ToolTraceJson: """
            [
              {
                "kind": "budget",
                "turn": 16,
                "conversation_chars": 20012,
                "enabled": true,
                "remaining": 0,
                "max": 16,
                "content": "Tool calls remaining: 0 of 16."
              }
            ]
            """,
         ToolRoundCount: 16,
         ConversationCharacterCount: 9886,
         OutputText: null,
         ErrorMessage: null,
         StartedAt: DateTimeOffset.UtcNow,
         CompletedAt: DateTimeOffset.UtcNow,
         DurationSeconds: 1m,
         InputTokens: null,
         OutputTokens: null,
         ReasoningTokens: null
      );

      Assert.Equal(20012, DetailsModel.GetMaxConversationCharacterCount(run));
   }

   [Fact]
   public void FormatTemperatureUsesRawRequestValue()
   {
      var run = new SESport.AI.Models.AiRunDetail(
         Id: Guid.NewGuid(),
         JobId: "job",
         JobLabel: "Job",
         PromptId: Guid.NewGuid(),
         PromptVersion: 1,
         SystemPrompt: "System",
         UserPromptTemplate: "User",
         ProviderId: "provider",
         ProviderLabel: "Provider",
         ProviderModel: "Model",
         StatusId: "completed",
         CorrelationId: null,
         InputPayloadJson: "{}",
         RenderedPrompt: "Rendered",
         RawRequestJson: """
            {
              "model": "test",
              "temperature": 0.73
            }
            """,
         RawResponseJson: null,
         ToolTraceJson: null,
         ToolRoundCount: 0,
         ConversationCharacterCount: 0,
         OutputText: null,
         ErrorMessage: null,
         StartedAt: DateTimeOffset.UtcNow,
         CompletedAt: DateTimeOffset.UtcNow,
         DurationSeconds: 1m,
         InputTokens: null,
         OutputTokens: null,
         ReasoningTokens: null
      );

      Assert.Equal("0.73", DetailsModel.FormatTemperature(run));
   }
}
