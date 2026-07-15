using SESport.Core.Domain;
using SESport.Web.Pages.Admin.Runs;

namespace SESport.Core.Tests.Pages.Admin.Runs;

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
   public void FormatToolCallSummaryIncludesUniqueCallCount()
   {
      Assert.Equal(
         $"{WebToolNames.GetPage} x 12 (8)",
         DetailsModel.FormatToolCallSummary(
            WebToolNames.GetPage,
            12,
            8
         )
      );
   }

   [Fact]
   public void GetToolBadgeCssClassHighlightsSubmitReport()
   {
      Assert.Equal(
         "tool-trace-badge tool-trace-badge-submit-report",
         DetailsModel.GetToolBadgeCssClass(WebToolNames.SubmitReport)
      );
   }

   [Fact]
   public void GetToolRoundCountUsesStoredValueOnly()
   {
      Assert.Equal(
         1,
         DetailsModel.GetToolRoundCount(1)
      );
   }

   [Fact]
   public void GetRenderedSystemPromptTextRendersPrimaryCountryTokens()
   {
      var run = new SESport.Core.AI.AiRunDetail(
         Id: Guid.NewGuid(),
         JobId: "job",
         JobLabel: "Job",
         PromptId: Guid.NewGuid(),
         PromptVersion: 1,
         SystemPrompt: "Process {{CountryName}} athletes.",
         UserPromptTemplate: "User",
         PromptTemperature: null,
         ProviderId: "provider",
         ProviderLabel: "Provider",
         ProviderModel: "Model",
         StatusId: "completed",
         CorrelationId: null,
         InputPayloadJson: "{}",
         RenderedPrompt: "Rendered",
         RawRequestJson: null,
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
         ReasoningTokens: null,
         ExecutionEnvironment: null,
         PromptMaxOutputTokens: null
      );

      Assert.Equal(
         $"Process {PrimaryCountry.CountryName} athletes.",
         DetailsModel.GetRenderedSystemPromptText(run)
      );
   }

   [Fact]
   public void BuildExecutionEnvironmentOptionsIncludesCurrentEnvironment()
   {
      var currentExecutionEnvironment =
         SESport.Core.AI.ExecutionEnvironment.Current;
      var options = DetailsModel.BuildExecutionEnvironmentOptions(
         ["Worker-A"],
         null,
         currentExecutionEnvironment
      );

      Assert.Contains(
         options,
         option => string.Equals(
            option.Value,
            currentExecutionEnvironment,
            StringComparison.Ordinal
         )
      );
   }

   [Fact]
   public void BuildExecutionEnvironmentOptionsMarksSelectedValue()
   {
      var options = DetailsModel.BuildExecutionEnvironmentOptions(
         ["Worker-A", "Worker-B"],
         "Worker-B",
         SESport.Core.AI.ExecutionEnvironment.Current,
         includeUnsetOption: false
      );

      Assert.Contains(
         options,
         option => string.Equals(
            option.Value,
            "Worker-B",
            StringComparison.Ordinal
         ) && option.Selected
      );

      Assert.Contains(
         options,
         option => string.Equals(
            option.Value,
            "Worker-B",
            StringComparison.Ordinal
         ) && string.Equals(
            option.Text,
            "Wor-B",
            StringComparison.Ordinal
         )
      );
   }

   [Fact]
   public void FormatExecutionEnvironmentDisplayNameUsesShortLabel()
   {
      Assert.Equal(
         "Dev-P53",
         DetailsModel.FormatExecutionEnvironmentDisplayName(
            "Development-THINKPAD-P53"
         )
      );
      Assert.Equal(
         "ABC",
         DetailsModel.FormatExecutionEnvironmentDisplayName("ABC")
      );
      Assert.Equal(
         "-",
         DetailsModel.FormatExecutionEnvironmentDisplayName(null)
      );
   }

   [Fact]
   public void GetMaxPayloadCharacterCountUsesRoundPeak()
   {
      var run = new SESport.Core.AI.AiRunDetail(
         Id: Guid.NewGuid(),
         JobId: "job",
         JobLabel: "Job",
         PromptId: Guid.NewGuid(),
         PromptVersion: 1,
         SystemPrompt: "System",
         UserPromptTemplate: "User",
         PromptTemperature: null,
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
                "payload_chars": 20012,
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
         ReasoningTokens: null,
         ExecutionEnvironment: null,
         PromptMaxOutputTokens: null
      );

      Assert.Equal(20012, DetailsModel.GetMaxPayloadCharacterCount(run));
   }

   [Fact]
   public void FormatTemperatureUsesPromptTemperature()
   {
      var run = new SESport.Core.AI.AiRunDetail(
         Id: Guid.NewGuid(),
         JobId: "job",
         JobLabel: "Job",
         PromptId: Guid.NewGuid(),
         PromptVersion: 1,
         SystemPrompt: "System",
         UserPromptTemplate: "User",
         PromptTemperature: 0.73m,
         ProviderId: "provider",
         ProviderLabel: "Provider",
         ProviderModel: "Model",
         StatusId: "completed",
         CorrelationId: null,
         InputPayloadJson: "{}",
         RenderedPrompt: "Rendered",
         RawRequestJson: null,
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
         ReasoningTokens: null,
         ExecutionEnvironment: null,
         PromptMaxOutputTokens: null
      );

      Assert.Equal("0.73", DetailsModel.FormatTemperature(run));
   }

   [Fact]
   public void FormatMaxOutputTokensUsesPromptSetting()
   {
      var run = new SESport.Core.AI.AiRunDetail(
         Id: Guid.NewGuid(),
         JobId: "job",
         JobLabel: "Job",
         PromptId: Guid.NewGuid(),
         PromptVersion: 1,
         SystemPrompt: "System",
         UserPromptTemplate: "User",
         PromptTemperature: null,
         ProviderId: "provider",
         ProviderLabel: "Provider",
         ProviderModel: "Model",
         StatusId: "completed",
         CorrelationId: null,
         InputPayloadJson: "{}",
         RenderedPrompt: "Rendered",
         RawRequestJson: null,
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
         ReasoningTokens: null,
         ExecutionEnvironment: null,
         PromptMaxOutputTokens: 8192
      );

      Assert.Equal("8192", DetailsModel.FormatMaxOutputTokens(run));
   }

   [Fact]
   public void FormatTemperatureReturnsNotSetWhenPromptTemperatureIsNull()
   {
      var run = new SESport.Core.AI.AiRunDetail(
         Id: Guid.NewGuid(),
         JobId: "job",
         JobLabel: "Job",
         PromptId: Guid.NewGuid(),
         PromptVersion: 1,
         SystemPrompt: "System",
         UserPromptTemplate: "User",
         PromptTemperature: null,
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
         ReasoningTokens: null,
         ExecutionEnvironment: null,
         PromptMaxOutputTokens: null
      );

      Assert.Equal("Not set", DetailsModel.FormatTemperature(run));
   }

   [Fact]
   public void FormatMaxOutputTokensReturnsNotSet()
   {
      var run = new SESport.Core.AI.AiRunDetail(
         Id: Guid.NewGuid(),
         JobId: "job",
         JobLabel: "Job",
         PromptId: Guid.NewGuid(),
         PromptVersion: 1,
         SystemPrompt: "System",
         UserPromptTemplate: "User",
         PromptTemperature: null,
         ProviderId: "provider",
         ProviderLabel: "Provider",
         ProviderModel: "Model",
         StatusId: "completed",
         CorrelationId: null,
         InputPayloadJson: "{}",
         RenderedPrompt: "Rendered",
         RawRequestJson: null,
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
         ReasoningTokens: null,
         ExecutionEnvironment: null,
         PromptMaxOutputTokens: null
      );

      Assert.Equal("Not set", DetailsModel.FormatMaxOutputTokens(run));
   }
}
