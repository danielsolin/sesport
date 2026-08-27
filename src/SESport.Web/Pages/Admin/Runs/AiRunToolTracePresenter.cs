using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using SESport.Core.Domain;

namespace SESport.Web.Pages.Admin.Runs;

public static class AiRunToolTracePresenter
{
   private const string CodexWebSearchActionName = "web_search";
   private const string CodexOtherActionType = "other";
   private const string CodexMissingSearchDetailsSummary =
      "No query or result reported";

   public static string FormatToolCall(ToolTraceCallViewModel toolCall)
   {
      var arguments = TryParseArguments(toolCall.Arguments);

      if(arguments is null || arguments.Count == 0)
      {
         return toolCall.Name;
      }

      var parts = new List<string>();
      var find = GetString(arguments, "find");
      var query = GetString(arguments, "query");
      var id = GetString(arguments, "id");
      var url = GetString(arguments, "url");
      var limit = GetInt32(arguments, "limit");

      if(string.Equals(
         toolCall.Name,
         WebToolNames.Search,
         StringComparison.Ordinal
      ))
      {
         if(!string.IsNullOrWhiteSpace(query))
         {
            parts.Add(FormatQuoted(query));
         }

         if(limit is not null)
         {
            parts.Add(limit.Value.ToString());
         }
      }
      else if(string.Equals(
         toolCall.Name,
         WebToolNames.FindInPage,
         StringComparison.Ordinal
      ) || string.Equals(
         toolCall.Name,
         WebToolNames.GetPage,
         StringComparison.Ordinal
      ))
      {
         if(!string.IsNullOrWhiteSpace(id))
         {
            parts.Add(FormatQuoted(id));
         }
         else if(!string.IsNullOrWhiteSpace(url))
         {
            parts.Add(FormatQuoted(url));
         }

         if(!string.IsNullOrWhiteSpace(find))
         {
            parts.Add(FormatQuoted(find));
         }
      }
      else
      {
         parts.Add(FormatCompactJson(arguments));
      }

      return parts.Count == 0
         ? toolCall.Name
         : $"{toolCall.Name}({string.Join(",", parts)})";
   }

   public static string FormatToolCallSummary(
      string toolName,
      int toolCallCount,
      int uniqueToolCallCount
   )
   {
      return $"{toolName} x {toolCallCount} ({uniqueToolCallCount})";
   }

   public static string FormatCodexActionSummary(
      ToolTraceCodexActionViewModel action
   )
   {
      if(!string.IsNullOrWhiteSpace(action.Command))
      {
         return action.Command.Trim();
      }

      if(string.Equals(
            action.Name,
            CodexWebSearchActionName,
            StringComparison.Ordinal
         ) &&
         string.Equals(
            action.ActionType,
            CodexOtherActionType,
            StringComparison.Ordinal
         ) &&
         string.IsNullOrWhiteSpace(action.Query))
      {
         return CodexMissingSearchDetailsSummary;
      }

      return string.IsNullOrWhiteSpace(action.Query)
         ? "Completed"
         : action.Query;
   }

   public static string FormatCodexActionResult(
      ToolTraceCodexActionViewModel action
   )
   {
      var parts = new List<string>();

      if(!string.IsNullOrWhiteSpace(action.Status))
      {
         parts.Add(action.Status);
      }

      if(action.ExitCode is not null)
      {
         parts.Add($"exit {action.ExitCode.Value}");
      }

      return string.Join(", ", parts);
   }

   public static string FormatCodexActionOutputCount(
      int? outputCharacterCount
   )
   {
      return outputCharacterCount?.ToString("N0") ?? "";
   }

   public static string GetToolBadgeCssClass(string toolName)
   {
      return string.Equals(
         toolName,
         WebToolNames.SubmitReport,
         StringComparison.Ordinal
      )
         ? "tool-trace-badge tool-trace-badge-submit-report"
         : "tool-trace-badge tool-trace-badge-tool";
   }

   public static IReadOnlyList<ToolTraceTurnViewModel> ParseToolTrace(
      string? toolTraceJson
   )
   {
      if(string.IsNullOrWhiteSpace(toolTraceJson))
      {
         return [];
      }

      try
      {
         using var document = JsonDocument.Parse(toolTraceJson);
         if(document.RootElement.ValueKind != JsonValueKind.Array)
         {
            return [];
         }

         var entries = document.RootElement.EnumerateArray().ToArray();

         if(IsCodexToolTrace(entries))
         {
            return ParseCodexToolTrace(entries);
         }

         var turns = new Dictionary<int, ToolTraceTurnBuilder>();

         foreach(var entry in entries)
         {
            if(entry.ValueKind != JsonValueKind.Object)
            {
               continue;
            }

            var kind = GetString(entry, "kind");
            var turn = GetInt32(entry, "turn") ?? 0;
            var builder = GetOrCreateTurn(turns, turn);
            var payloadChars = GetInt32(entry, "payload_chars");

            if(payloadChars is not null)
            {
               builder.RoundPayloadCharacterCount = Math.Max(
                  builder.RoundPayloadCharacterCount ?? 0,
                  payloadChars.Value
               );
            }

            if(string.Equals(kind, "assistant", StringComparison.Ordinal))
            {
               builder.FinishReason = GetString(entry, "finish_reason");
               builder.AssistantContent = GetString(entry, "content");
               builder.AssistantReasoningContent =
                  GetString(entry, "reasoning_content");
               builder.AssistantValidationStatus =
                  GetString(entry, "validation_status");
               builder.AssistantValidationError =
                  GetString(entry, "validation_error");
               builder.ToolCalls.AddRange(ParseToolCalls(entry));
               continue;
            }

            if(string.Equals(kind, "budget", StringComparison.Ordinal))
            {
               builder.Temperature ??= GetDecimal(entry, "temperature");
               continue;
            }

            if(string.Equals(kind, "tool", StringComparison.Ordinal))
            {
               builder.ToolResults.Add(ParseToolResult(entry));
               continue;
            }

            if(string.Equals(kind, "submission", StringComparison.Ordinal))
            {
               builder.Submissions.Add(ParseToolSubmission(entry));
               continue;
            }

            if(string.Equals(
               kind,
               "conditional_tools",
               StringComparison.Ordinal
            ))
            {
               builder.ConditionalTools.AddRange(ParseConditionalTools(entry));
               continue;
            }

            if(string.Equals(kind, "repair_prompt", StringComparison.Ordinal))
            {
               var repairPrompt = GetString(entry, "content");

               if(!string.IsNullOrWhiteSpace(repairPrompt))
               {
                  builder.RepairPrompts.Add(repairPrompt);
               }

               continue;
            }

            if(string.Equals(
               kind,
               "tool_format_fallback",
               StringComparison.Ordinal
            ))
            {
               var note = ParseToolTraceNote(
                  entry,
                  "Tool format fallback",
                  "reason"
               );

               if(note is not null)
               {
                  builder.Notes.Add(note);
               }

               continue;
            }

            if(string.Equals(
               kind,
               "validation_feedback",
               StringComparison.Ordinal
            ))
            {
               var note = ParseToolTraceNote(
                  entry,
                  "Validation feedback",
                  "validation_error"
               );

               if(note is not null)
               {
                  builder.Notes.Add(note);
               }
            }
         }

         return turns
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value.ToViewModel())
            .ToArray();
      }
      catch(JsonException)
      {
         return [];
      }
   }

   private static bool IsCodexToolTrace(
      IReadOnlyList<JsonElement> entries
   )
   {
      return entries.Any(entry =>
         string.Equals(
            GetString(entry, "type"),
            "thread.started",
            StringComparison.Ordinal
         ) || GetProperty(entry, "item") is
         { ValueKind: JsonValueKind.Object }
      );
   }

   private static IReadOnlyList<ToolTraceTurnViewModel>
      ParseCodexToolTrace(IReadOnlyList<JsonElement> entries)
   {
      var actionTurns = new List<ToolTraceTurnBuilder>();
      var diagnosticNotes = new List<ToolTraceNoteViewModel>();
      var usageNotes = new List<ToolTraceNoteViewModel>();
      string? assistantContent = null;

      foreach(var entry in entries)
      {
         var eventType = GetString(entry, "type");

         if(string.Equals(
            eventType,
            "item.completed",
            StringComparison.Ordinal
         ))
         {
            var item = GetProperty(entry, "item");

            if(item is null || item.Value.ValueKind != JsonValueKind.Object)
            {
               continue;
            }

            var itemType = GetString(item.Value, "type");

            if(string.Equals(
               itemType,
               "agent_message",
               StringComparison.Ordinal
            ))
            {
               assistantContent = GetString(item.Value, "text");
               continue;
            }

            if(string.Equals(
               itemType,
               "error",
               StringComparison.Ordinal
            ))
            {
               var message = GetString(item.Value, "message");

               if(!string.IsNullOrWhiteSpace(message))
               {
                  diagnosticNotes.Add(new ToolTraceNoteViewModel(
                     "Codex diagnostic",
                     message,
                     null
                  ));
               }

               continue;
            }

            if(string.IsNullOrWhiteSpace(itemType))
            {
               continue;
            }

            var actionTurn = new ToolTraceTurnBuilder(
               actionTurns.Count + 1
            );
            actionTurn.CodexActions.Add(ParseCodexAction(item.Value));
            actionTurns.Add(actionTurn);
            continue;
         }

         if(string.Equals(
            eventType,
            "turn.completed",
            StringComparison.Ordinal
         ))
         {
            var usage = GetProperty(entry, "usage");

            if(usage is not null)
            {
               usageNotes.Add(new ToolTraceNoteViewModel(
                  "Codex usage",
                  FormatDisplayValue(usage.Value),
                  null
               ));
            }
         }
      }

      if(actionTurns.Count == 0)
      {
         var assistantTurn = new ToolTraceTurnBuilder(1)
         {
            AssistantContent = assistantContent
         };
         assistantTurn.Notes.AddRange(diagnosticNotes);
         assistantTurn.Notes.AddRange(usageNotes);
         return [assistantTurn.ToViewModel()];
      }

      var finalTurn = actionTurns[^1];
      finalTurn.AssistantContent = assistantContent;
      finalTurn.Notes.AddRange(diagnosticNotes);
      finalTurn.Notes.AddRange(usageNotes);

      return actionTurns
         .Select(turn => turn.ToViewModel())
         .ToArray();
   }

   private static ToolTraceCodexActionViewModel ParseCodexAction(
      JsonElement item
   )
   {
      var action = GetProperty(item, "action");
      var actionType = action is null
         ? null
         : GetString(action.Value, "type");

      return new ToolTraceCodexActionViewModel(
         GetString(item, "type") ?? "codex_action",
         GetString(item, "id"),
         GetString(item, "query"),
         actionType,
         ParseCodexSearchQueries(action),
         GetString(item, "command"),
         GetString(item, "status"),
         GetInt32(item, "exit_code"),
         GetString(item, "aggregated_output")?.Length,
         GetString(item, "aggregated_output"),
         FormatDisplayValue(item)
      );
   }

   private static IReadOnlyList<string> ParseCodexSearchQueries(
      JsonElement? action
   )
   {
      if(action is null ||
         !TryGetArray(action.Value, "queries", out var queries))
      {
         return [];
      }

      return queries
         .Where(query => query.ValueKind == JsonValueKind.String)
         .Select(query => query.GetString() ?? string.Empty)
         .Where(query => !string.IsNullOrWhiteSpace(query))
         .ToArray();
   }

   public static IReadOnlyList<ToolTraceBadgeViewModel>
      BuildToolTraceSummaryBadges(
         IReadOnlyList<ToolTraceTurnViewModel> turns
      )
   {
      if(turns.Count == 0)
      {
         return [];
      }

      var badges = new List<ToolTraceBadgeViewModel>
      {
         new(
            $"{turns.Count} round{(turns.Count == 1 ? "" : "s")}",
            "tool-trace-badge-round"
         )
      };

      var toolCalls = turns
         .SelectMany(turn => turn.ToolCalls)
         .GroupBy(call => call.Name, StringComparer.Ordinal)
         .OrderBy(group => group.Key, StringComparer.Ordinal)
         .ToArray();

      foreach(var toolCallGroup in toolCalls)
      {
         var uniqueToolCallCount = toolCallGroup
            .Select(FormatToolCall)
            .Distinct(StringComparer.Ordinal)
            .Count();
         badges.Add(new(
            FormatToolCallSummary(
               toolCallGroup.Key,
               toolCallGroup.Count(),
               uniqueToolCallCount
            ),
            GetToolBadgeCssClass(toolCallGroup.Key)
         ));
      }

      var submissions = turns
         .SelectMany(turn => turn.Submissions)
         .GroupBy(submission => submission.Name, StringComparer.Ordinal)
         .OrderBy(group => group.Key, StringComparer.Ordinal)
         .ToArray();

      foreach(var submissionGroup in submissions)
      {
         badges.Add(new(
            FormatSubmissionBadgeText(
               submissionGroup.Key,
               submissionGroup.Count()
            ),
            GetToolBadgeCssClass(submissionGroup.Key)
         ));
      }

      var conditionalTools = turns
         .SelectMany(turn => turn.ConditionalTools)
         .GroupBy(tool => (tool.Name, tool.Behavior))
         .OrderBy(group => group.Key.Name, StringComparer.Ordinal)
         .Select(group => group.First())
         .ToArray();

      if(conditionalTools.Length > 0)
      {
         badges.Add(new(
            BuildConditionalToolsBadgeText(conditionalTools),
            GetConditionalToolBadgeCssClass(conditionalTools)
         ));
      }

      var totalToolResults = turns.Sum(turn => turn.ToolResults.Count);
      if(totalToolResults > 0)
      {
         badges.Add(new(
            $"{totalToolResults} result{(totalToolResults == 1 ? "" : "s")}",
            "tool-trace-badge-result"
         ));
      }

      var finalTurn = turns[^1];
      if(!string.IsNullOrWhiteSpace(finalTurn.FinishReason))
      {
         badges.Add(new(
            $"Finish: {finalTurn.FinishReason}",
            "tool-trace-badge-finish"
         ));
      }

      return badges;
   }

   private static string BuildConditionalToolsBadgeText(
      IReadOnlyList<ToolTraceConditionalToolViewModel> conditionalTools
   )
   {
      var toolNames = conditionalTools
         .Select(tool => string.IsNullOrWhiteSpace(tool.Behavior) ||
               string.Equals(
                  tool.Behavior,
                  tool.Name,
                  StringComparison.Ordinal
               )
            ? tool.Name
            : $"{tool.Name} ({tool.Behavior})")
         .ToArray();

      return $"Conditional: {string.Join(", ", toolNames)}";
   }

   private static string FormatSubmissionBadgeText(
      string name,
      int count
   )
   {
      return count == 1
         ? name
         : $"{name} × {count}";
   }

   private static string GetConditionalToolBadgeCssClass(
      IReadOnlyList<ToolTraceConditionalToolViewModel> conditionalTools
   )
   {
      return conditionalTools.Any(tool =>
         string.Equals(
            tool.Behavior,
            WebToolNames.SubmitReport,
            StringComparison.Ordinal
         ) || string.Equals(
            tool.Name,
            WebToolNames.SubmitReport,
            StringComparison.Ordinal
         ))
         ? "tool-trace-badge tool-trace-badge-submit-report"
         : "tool-trace-badge tool-trace-badge-tool";
   }

   private static ToolTraceTurnBuilder GetOrCreateTurn(
      IDictionary<int, ToolTraceTurnBuilder> turns,
      int turn
   )
   {
      if(!turns.TryGetValue(turn, out var builder))
      {
         builder = new ToolTraceTurnBuilder(turn);
         turns[turn] = builder;
      }

      return builder;
   }

   private static IReadOnlyList<ToolTraceCallViewModel> ParseToolCalls(
      JsonElement entry
   )
   {
      if(!TryGetArray(entry, "tool_calls", out var toolCalls))
      {
         return [];
      }

      return toolCalls
         .Where(call => call.ValueKind == JsonValueKind.Object)
         .Select(call => new ToolTraceCallViewModel(
            GetString(call, "id") ?? "",
            GetString(call, "name") ?? "",
            FormatDisplayValue(GetProperty(call, "arguments"))
         ))
         .ToArray();
   }

   private static IReadOnlyList<ToolTraceConditionalToolViewModel>
      ParseConditionalTools(JsonElement entry)
   {
      if(!TryGetArray(entry, "conditional_tools", out var conditionalTools))
      {
         return [];
      }

      return conditionalTools
         .Where(tool => tool.ValueKind == JsonValueKind.Object)
         .Select(tool => new ToolTraceConditionalToolViewModel(
            GetString(tool, "name") ?? "",
            GetString(tool, "behavior")
         ))
         .Where(tool => !string.IsNullOrWhiteSpace(tool.Name))
         .ToArray();
   }

   private static ToolTraceToolResultViewModel ParseToolResult(
      JsonElement entry
   )
   {
      return new ToolTraceToolResultViewModel(
         GetString(entry, "tool_call_id") ?? "",
         GetString(entry, "name") ?? "",
         FormatDisplayValue(GetProperty(entry, "arguments")),
         GetString(entry, "query"),
         GetString(entry, "id"),
         GetString(entry, "search_provider"),
         GetString(entry, "search_provider_details"),
         GetString(entry, "search_engine"),
         GetString(entry, "fetcher"),
         GetString(entry, "browser_strategy"),
         FormatDisplayValue(GetProperty(entry, "result"))
      );
   }

   private static ToolTraceSubmissionViewModel ParseToolSubmission(
      JsonElement entry
   )
   {
      return new ToolTraceSubmissionViewModel(
         GetString(entry, "tool_call_id") ?? "",
         GetString(entry, "name") ?? "",
         GetString(entry, "arguments")
      );
   }

   private static string? GetString(JsonElement element, string name)
   {
      if(!element.TryGetProperty(name, out var property))
      {
         return null;
      }

      return property.ValueKind == JsonValueKind.String
         ? property.GetString()
         : property.ToString();
   }

   private static int? GetInt32(JsonElement element, string name)
   {
      if(!element.TryGetProperty(name, out var property))
      {
         return null;
      }

      return property.ValueKind == JsonValueKind.Number &&
         property.TryGetInt32(out var value)
         ? value
         : null;
   }

   private static decimal? GetDecimal(JsonElement element, string name)
   {
      if(!element.TryGetProperty(name, out var property))
      {
         return null;
      }

      return property.ValueKind == JsonValueKind.Number &&
         property.TryGetDecimal(out var value)
         ? value
         : null;
   }

   private static bool TryGetArray(
      JsonElement element,
      string name,
      out IEnumerable<JsonElement> values
   )
   {
      values = [];

      if(!element.TryGetProperty(name, out var property) ||
         property.ValueKind != JsonValueKind.Array)
      {
         return false;
      }

      values = property.EnumerateArray().ToArray();
      return true;
   }

   private static JsonElement? GetProperty(JsonElement element, string name)
   {
      return element.TryGetProperty(name, out var property) ? property : null;
   }

   private static JsonObject? TryParseArguments(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return null;
      }

      try
      {
         return JsonDocument.Parse(arguments).RootElement.ValueKind ==
            JsonValueKind.Object
            ? JsonSerializer.Deserialize<JsonObject>(arguments)
            : null;
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static string? GetString(JsonObject? element, string name)
   {
      if(element is null || !element.TryGetPropertyValue(name, out var value))
      {
         return null;
      }

      return value is JsonValue jsonValue &&
         jsonValue.TryGetValue<string>(out var text)
         ? text
         : value?.ToString();
   }

   private static int? GetInt32(JsonObject? element, string name)
   {
      if(element is null || !element.TryGetPropertyValue(name, out var value))
      {
         return null;
      }

      return value is JsonValue jsonValue &&
         jsonValue.TryGetValue<int>(out var number)
         ? number
         : null;
   }

   private static string FormatQuoted(string value)
   {
      return "'" + value.Replace("'", "\\'") + "'";
   }

   private static string FormatCompactJson(JsonObject value)
   {
      return JsonSerializer.Serialize(value);
   }

   private static ToolTraceNoteViewModel? ParseToolTraceNote(
      JsonElement entry,
      string title,
      string detailProperty
   )
   {
      var content = GetString(entry, "content")?.Trim();
      var detail = GetString(entry, detailProperty)?.Trim();

      if(string.IsNullOrWhiteSpace(content) &&
         string.IsNullOrWhiteSpace(detail))
      {
         return null;
      }

      return new ToolTraceNoteViewModel(
         title,
         content ?? string.Empty,
         detail
      );
   }

   private static string FormatDisplayValue(JsonElement? value)
   {
      if(value is null)
      {
         return "";
      }

      var text = value.Value.ValueKind switch
      {
         JsonValueKind.String => value.Value.GetString() ?? "",
         JsonValueKind.Null => "",
         _ => value.Value.GetRawText()
      };

      if(AiRunJsonFormatter.TryPrettyPrint(
            text,
            out var prettyPrinted
         ))
      {
         return prettyPrinted;
      }

      return text.Trim();
   }

   public sealed record ToolTraceCallViewModel(
      string Id,
      string Name,
      string Arguments
   );

   public sealed record ToolTraceBadgeViewModel(
      string Text,
      string CssClass
   );

   public sealed record ToolTraceToolResultViewModel(
      string ToolCallId,
      string Name,
      string Arguments,
      string? Query,
      string? Id,
      string? SearchProvider,
      string? SearchProviderDetails,
      string? SearchEngine,
      string? Fetcher,
      string? BrowserStrategy,
      string Result
   );

   public sealed record ToolTraceCodexActionViewModel(
      string Name,
      string? Id,
      string? Query,
      string? ActionType,
      IReadOnlyList<string> SearchQueries,
      string? Command,
      string? Status,
      int? ExitCode,
      int? OutputCharacterCount,
      string? AggregatedOutput,
      string RawJson
   );

   public sealed record ToolTraceSubmissionViewModel(
      string ToolCallId,
      string Name,
      string? Arguments
   );

   public sealed record ToolTraceNoteViewModel(
      string Title,
      string Content,
      string? Detail
   );

   public sealed record ToolTraceConditionalToolViewModel(
      string Name,
      string? Behavior
   );

   public sealed record ToolTraceTurnViewModel(
      int Turn,
      int? RoundPayloadCharacterCount,
      decimal? Temperature,
      IReadOnlyList<ToolTraceConditionalToolViewModel> ConditionalTools,
      string? FinishReason,
      string? AssistantContent,
      string? AssistantReasoningContent,
      string? AssistantValidationStatus,
      string? AssistantValidationError,
      IReadOnlyList<string> RepairPrompts,
      IReadOnlyList<ToolTraceCallViewModel> ToolCalls,
      IReadOnlyList<ToolTraceSubmissionViewModel> Submissions,
      IReadOnlyList<ToolTraceNoteViewModel> Notes,
      IReadOnlyList<ToolTraceToolResultViewModel> ToolResults,
      IReadOnlyList<ToolTraceCodexActionViewModel> CodexActions,
      IReadOnlyList<ToolTraceBadgeViewModel> CompactBadges,
      string? AssistantPreview
   );

   private sealed class ToolTraceTurnBuilder(int turn)
   {
      public int Turn { get; } = turn;

      public int? RoundPayloadCharacterCount { get; set; }

      public decimal? Temperature { get; set; }

      public List<ToolTraceConditionalToolViewModel> ConditionalTools
      { get; } = [];

      public string? FinishReason { get; set; }

      public string? AssistantContent { get; set; }

      public string? AssistantReasoningContent { get; set; }

      public string? AssistantValidationStatus { get; set; }

      public string? AssistantValidationError { get; set; }

      public List<string> RepairPrompts { get; } = [];

      public List<ToolTraceCallViewModel> ToolCalls { get; } = [];

      public List<ToolTraceSubmissionViewModel> Submissions { get; } = [];

      public List<ToolTraceNoteViewModel> Notes { get; } = [];

      public List<ToolTraceToolResultViewModel> ToolResults { get; } = [];

      public List<ToolTraceCodexActionViewModel> CodexActions
      { get; } = [];

      public ToolTraceTurnViewModel ToViewModel()
      {
         return new ToolTraceTurnViewModel(
            Turn,
            RoundPayloadCharacterCount,
            Temperature,
            ConditionalTools,
            FinishReason,
            AssistantContent,
            AssistantReasoningContent,
            AssistantValidationStatus,
            AssistantValidationError,
            RepairPrompts,
            ToolCalls,
            Submissions,
            Notes,
            ToolResults,
            CodexActions,
            BuildCompactBadges(),
            BuildAssistantPreview()
         );
      }

      private IReadOnlyList<ToolTraceBadgeViewModel> BuildCompactBadges()
      {
         var badges = new List<ToolTraceBadgeViewModel>
         {
            new($"Round {Turn}", "tool-trace-badge-round")
         };

         if(RoundPayloadCharacterCount is not null)
         {
            badges.Add(new(
               $"Payload chars " +
               $"{RoundPayloadCharacterCount.Value:N0}",
               "tool-trace-badge-count"
            ));
         }

         if(Temperature is not null)
         {
            badges.Add(new(
               $"Temp " +
               $"{Temperature.Value.ToString(CultureInfo.InvariantCulture)}",
               "tool-trace-badge-temperature"
            ));
         }

         if(ConditionalTools.Count > 0)
         {
            badges.Add(new(
               BuildConditionalToolsBadgeText(ConditionalTools),
               GetConditionalToolBadgeCssClass(ConditionalTools)
            ));
         }

         if(!string.IsNullOrWhiteSpace(AssistantContent))
         {
            badges.Add(new("Assistant", "tool-trace-badge-assistant"));
         }

         if(!string.IsNullOrWhiteSpace(AssistantReasoningContent))
         {
            badges.Add(new("Reasoning", "tool-trace-badge-assistant"));
         }

         if(!string.IsNullOrWhiteSpace(AssistantValidationStatus))
         {
            badges.Add(new(
               $"Validation: {AssistantValidationStatus}",
               "tool-trace-badge-result"
            ));
         }

         foreach(var toolCallGroup in ToolCalls
            .GroupBy(call => call.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
         {
            badges.Add(new(
               $"{toolCallGroup.Key} × {toolCallGroup.Count()}",
               GetToolBadgeCssClass(toolCallGroup.Key)
            ));
         }

         foreach(var submissionGroup in Submissions
            .GroupBy(submission => submission.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
         {
            badges.Add(new(
               FormatSubmissionBadgeText(
                  submissionGroup.Key,
                  submissionGroup.Count()
               ),
               GetToolBadgeCssClass(submissionGroup.Key)
            ));
         }

         if(ToolResults.Count > 0)
         {
            badges.Add(new(
               $"{ToolResults.Count} result{(ToolResults.Count == 1 ? "" :
                  "s")}",
               "tool-trace-badge-result"
            ));
         }

         if(!string.IsNullOrWhiteSpace(FinishReason))
         {
            badges.Add(new(
               $"Finish: {FinishReason}",
               "tool-trace-badge-finish"
            ));
         }

         return badges;
      }

      private string? BuildAssistantPreview()
      {
         return BuildCompactPreview(AssistantContent, 120);
      }
   }

   private static string? BuildCompactPreview(
      string? value,
      int maxLength
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      var text = string.Join(
         " ",
         value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
      ).Trim();

      if(text.Length <= maxLength)
      {
         return text;
      }

      return text[..(maxLength - 3)] + "...";
   }
}
