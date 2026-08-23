using SESport.Core.Configuration;
using SESport.Web.Pages.Admin.Runs;
using System.Globalization;

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
   public void FormatCodexActionSummaryExplainsMissingSearchDetails()
   {
      var action = new DetailsModel.ToolTraceCodexActionViewModel(
         "web_search",
         "search-1",
         null,
         "other",
         [],
         null,
         null,
         null,
         null,
         null,
         "{}"
      );

      Assert.Equal(
         "No query or result reported",
         DetailsModel.FormatCodexActionSummary(action)
      );
   }

   [Fact]
   public void FormatJsonOrRetentionNoticeExplainsPurgedPayload()
   {
      Assert.Equal(
         "Removed by retention policy.",
         DetailsModel.FormatJsonOrRetentionNotice(
            null,
            DateTimeOffset.UtcNow
         )
      );
      Assert.Equal(
         "{\n  \"value\": 1\n}",
         DetailsModel.FormatJsonOrRetentionNotice(
            "{\"value\": 1}",
            null
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
   public void ParseToolTraceIncludesFallbackNote()
   {
      var reason =
         "The model produced output that does not match the expected format.";
      var content =
         "Tool request failed in llama-server structured output parsing. " +
         "Retrying with tools.";

      var turns = DetailsModel.ParseToolTrace(
         $$"""
         [
           {
             "kind": "budget",
             "turn": 7,
             "content": "Tool calls remaining: 9 of 15.",
             "enabled": true,
             "remaining": 9,
             "max": 15,
             "temperature": 1.0,
             "payload_chars": 21122,
             "conditional_tools": [
               {
                 "name": "submit_report",
                 "behavior": "submit_report"
               }
             ]
           },
           {
             "kind": "tool_format_fallback",
             "turn": 7,
             "reason": "{{reason}}",
             "content": "{{content}}"
           }
         ]
         """
      );

      var turn = Assert.Single(turns);

      Assert.Equal(7, turn.Turn);
      var note = Assert.Single(turn.Notes);
      Assert.Equal("Tool format fallback", note.Title);
      Assert.Equal(
         "Tool request failed in llama-server structured output parsing. " +
         "Retrying with tools.",
         note.Content
      );
      Assert.Equal(
         "The model produced output that does not match the expected " +
         "format.",
         note.Detail
      );
   }

   [Fact]
   public void ParseToolTraceParsesCodexJsonlEvents()
   {
      var turns = DetailsModel.ParseToolTrace(
         """
         [
           {
             "type": "thread.started",
             "thread_id": "thread-1"
           },
           {
             "type": "turn.started"
           },
           {
             "type": "item.completed",
             "item": {
               "id": "item-1",
               "type": "agent_message",
               "text": "Initial response"
             }
           },
           {
             "type": "item.completed",
             "item": {
               "id": "exec-1",
               "type": "web_search",
               "query": "Sweden Montenegro",
               "action": {
                 "type": "search",
                 "queries": ["Sweden Montenegro"]
               }
             }
           },
           {
             "type": "item.completed",
             "item": {
               "id": "exec-2",
               "type": "web_search",
               "query": "Montenegro Sweden",
               "action": {
                 "type": "search",
                 "queries": ["Montenegro Sweden"]
               }
             }
           },
           {
             "type": "item.completed",
             "item": {
               "id": "exec-3",
               "type": "command_execution",
               "command": "/bin/bash -lc \"printf 'done\\n'\"",
               "status": "completed",
               "exit_code": 0,
               "aggregated_output": "done\n"
             }
           },
           {
             "type": "item.completed",
             "item": {
               "id": "item-2",
               "type": "agent_message",
               "text": "Final response"
             }
           },
           {
             "type": "turn.completed",
             "usage": {
               "input_tokens": 12,
               "output_tokens": 8
             }
           }
         ]
         """
      );

      Assert.Equal(3, turns.Count);
      var firstTurn = turns[0];
      var secondTurn = turns[1];
      var finalTurn = turns[2];
      Assert.Equal(1, firstTurn.Turn);
      Assert.Equal(3, finalTurn.Turn);
      Assert.Null(firstTurn.AssistantContent);
      Assert.Equal("Final response", finalTurn.AssistantContent);

      var action = Assert.Single(firstTurn.CodexActions);
      Assert.Equal("web_search", action.Name);
      Assert.Equal("Sweden Montenegro", action.Query);
      Assert.Equal("search", action.ActionType);
      Assert.Single(action.SearchQueries);
      Assert.Contains("\"queries\"", action.RawJson);

      var secondAction = Assert.Single(secondTurn.CodexActions);
      Assert.Equal("Montenegro Sweden", secondAction.Query);

      var commandAction = Assert.Single(finalTurn.CodexActions);
      Assert.Equal("command_execution", commandAction.Name);
      Assert.Equal("/bin/bash -lc \"printf 'done\\n'\"",
         commandAction.Command);
      Assert.Equal("completed", commandAction.Status);
      Assert.Equal(0, commandAction.ExitCode);
      Assert.Equal(5, commandAction.OutputCharacterCount);
      Assert.Equal("done\n", commandAction.AggregatedOutput);
      Assert.Equal(
         commandAction.Command,
         DetailsModel.FormatCodexActionSummary(commandAction)
      );
      Assert.Equal(
         "completed, exit 0",
         DetailsModel.FormatCodexActionResult(commandAction)
      );
      Assert.Equal(
         "5",
         DetailsModel.FormatCodexActionOutputCount(
            commandAction.OutputCharacterCount
         )
      );

      var note = Assert.Single(finalTurn.Notes);
      Assert.Equal("Codex usage", note.Title);
      Assert.Contains("\"input_tokens\": 12", note.Content);
   }

   [Fact]
   public void ParseToolTraceRendersCodexErrorEventsAsDiagnostics()
   {
      var turns = DetailsModel.ParseToolTrace(
         """
         [
           {
             "type": "thread.started",
             "thread_id": "thread-1"
           },
           {
             "type": "item.completed",
             "item": {
               "id": "item-0",
               "type": "error",
               "message": "Model metadata for `qwen3.8-27b` not found."
             }
           },
           {
             "type": "item.completed",
             "item": {
               "id": "exec-1",
               "type": "command_execution",
               "command": "printf 'done\\n'",
               "status": "completed",
               "exit_code": 0,
               "aggregated_output": "done\\n"
             }
           }
         ]
         """
      );

      var turn = Assert.Single(turns);
      var action = Assert.Single(turn.CodexActions);
      Assert.Equal("command_execution", action.Name);
      Assert.Equal(1, turn.Turn);
      var note = Assert.Single(turn.Notes);
      Assert.Equal("Codex diagnostic", note.Title);
      Assert.Equal(
         "Model metadata for `qwen3.8-27b` not found.",
         note.Content
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
      var run = CreateRun() with
      {
         SystemPrompt = "Process {{CountryName}} athletes.",
         RenderedSystemPrompt =
            $"Process {PrimaryCountry.CountryName} athletes."
      };

      Assert.Equal(
         $"Process {PrimaryCountry.CountryName} athletes.",
         DetailsModel.GetRenderedSystemPromptText(run)
      );
   }

   [Fact]
   public void BuildExecutionEnvironmentOptionsIncludesCurrentEnvironment()
   {
      var currentExecutionEnvironment =
         SESport.Core.Configuration.ExecutionEnvironment.Current;
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
         SESport.Core.Configuration.ExecutionEnvironment.Current,
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
      var run = CreateRun() with
      {
         ToolTraceJson = """
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
         ToolRoundCount = 16,
         ConversationCharacterCount = 9886
      };

      Assert.Equal(20012, DetailsModel.GetMaxPayloadCharacterCount(run));
   }

   [Fact]
   public void FormatTemperatureUsesPromptTemperature()
   {
      var run = CreateRun() with
      {
         PromptTemperature = 0.73m
      };

      Assert.Equal("0.73", DetailsModel.FormatTemperature(run));
   }

   [Fact]
   public void FormatMaxOutputTokensUsesStoredValue()
   {
      var run = CreateRun() with
      {
         MaxOutputTokens = 4096
      };

      Assert.Equal("4096", DetailsModel.FormatMaxOutputTokens(run));
   }

   [Fact]
   public void BuildTokenUsageCalculatesUncachedInputTokens()
   {
      var run = CreateRun() with
      {
         InputTokens = 1033255,
         OutputTokens = 6199,
         RawResponseJson =
            """
            {
              "usage": {
                "input_tokens": 1033255,
                "output_tokens": 6199,
                "cached_input_tokens": 896768,
                "reasoning_output_tokens": 4508,
                "cache_write_input_tokens": 0
              }
            }
            """
      };

      var usage = DetailsModel.BuildTokenUsage(run);

      Assert.NotNull(usage);
      Assert.Equal(1033255, usage!.InputTokens);
      Assert.Equal(896768, usage.CachedInputTokens);
      Assert.Equal(136487, usage.UncachedInputTokens);
      Assert.Equal(0, usage.CacheWriteInputTokens);
      Assert.Equal(6199, usage.OutputTokens);
      Assert.Equal(4508, usage.ReasoningTokens);
   }

   [Fact]
   public void FormatTokenCountUsesSwedishThousandsSeparators()
   {
      Assert.Equal(
         1033255.ToString(
            "N0",
            CultureInfo.GetCultureInfo(PrimaryCountry.CultureName)
         ),
         DetailsModel.FormatTokenCount(1033255)
      );
   }

   [Fact]
   public void FormatTemperatureReturnsNotSetWhenPromptTemperatureIsNull()
   {
      var run = CreateRun() with
      {
         RawRequestJson = """
            {
              "model": "test",
              "temperature": 0.73
            }
            """,
         PromptTemperature = null
      };

      Assert.Equal("Not set", DetailsModel.FormatTemperature(run));
   }

   [Fact]
   public void FormatMaxOutputTokensUsesStoredDefaultValue()
   {
      var run = CreateRun();

      Assert.Equal(
         AiDefaults.DefaultMaxOutputTokens.ToString(
            CultureInfo.InvariantCulture
         ),
         DetailsModel.FormatMaxOutputTokens(run)
      );
   }

   [Fact]
   public void FormatMaxToolRoundsUsesStoredValue()
   {
      var run = CreateRun() with
      {
         PromptMaxToolRounds = 4
      };

      Assert.Equal("4", DetailsModel.FormatMaxToolRounds(run));
   }

   [Fact]
   public void FormatMaxToolRoundsUsesDefaultForWebSearchWhenNull()
   {
      Assert.Equal("10", DetailsModel.FormatMaxToolRounds(CreateRun()));
   }

   [Fact]
   public void FormatMaxToolRoundsReturnsNotSetWithoutWebSearch()
   {
      var run = CreateRun() with
      {
         JobRequiresWebSearch = false
      };

      Assert.Equal("Not set", DetailsModel.FormatMaxToolRounds(run));
   }

   [Fact]
   public void FormatMinToolRoundsUsesStoredValue()
   {
      var run = CreateRun() with
      {
         PromptMinToolRounds = 15
      };

      Assert.Equal("15", DetailsModel.FormatMinToolRounds(run));
   }

   [Fact]
   public void FormatMinToolRoundsReturnsNotSetWhenNull()
   {
      Assert.Equal(
         "Not set",
         DetailsModel.FormatMinToolRounds(CreateRun())
      );
   }

   private static SESport.Core.AI.AiRunDetail CreateRun()
   {
      return new SESport.Core.AI.AiRunDetail(
         Id: Guid.NewGuid(),
         JobId: "job",
         JobLabel: "Job",
         PromptId: Guid.NewGuid(),
         PromptVersion: 1,
         SystemPrompt: "System",
         UserPromptTemplate: "User",
         PromptTemperature: null,
         PromptMaxOutputTokens: null,
         PromptMaxToolRounds: null,
         MaxOutputTokens: AiDefaults.DefaultMaxOutputTokens,
         PromptOutputSchemaJson: null,
         PromptRequestOptionsJson: "{}",
         ProviderId: "provider",
         ProviderLabel: "Provider",
         ProviderKind: "llama-server",
         ProviderBaseAddress: null,
         ProviderModel: "Model",
         ProviderApiKeySource: null,
         ProviderRequestOptionsJson: "{}",
         StatusId: "completed",
         CorrelationId: null,
         InputPayloadJson: "{}",
         RenderedSystemPrompt: "Rendered System",
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
         JobOutputMode: "text",
         JobRequiresWebSearch: true,
         JobToolsJson: null,
         JobConditionalToolsJson: null,
         JobToolCallMaxTokens: null
      );
   }
}
