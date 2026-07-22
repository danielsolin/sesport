using System.Text.Json;
using System.Text.Json.Nodes;

using SESport.Core.Configuration;

namespace SESport.AI.Llama;

internal static class LlamaToolTrace
{
   public static JsonObject CreateAssistantTraceEntry(
      int turn,
      JsonObject response,
      IReadOnlyList<LlamaToolCall> toolCalls,
      JsonSerializerOptions jsonOptions,
      string? validationStatus = null,
      string? validationError = null
   )
   {
      return new JsonObject
      {
         ["kind"] = "assistant",
         ["turn"] = turn,
         ["finish_reason"] = LlamaResponseReader.GetFinishReason(response),
         ["content"] = LlamaResponseReader.NormalizeOutput(
            LlamaResponseReader.ExtractFinalText(response, jsonOptions)
         ),
         ["reasoning_content"] =
            LlamaResponseReader.ExtractReasoningContent(response),
         ["validation_status"] = validationStatus,
         ["validation_error"] = validationError,
         ["tool_calls"] = JsonSerializer.SerializeToNode(
            toolCalls.Select(toolCall => new
            {
               id = toolCall.Id,
               name = toolCall.Name,
               arguments = toolCall.Arguments
            }).ToArray(),
            jsonOptions
         )
      };
   }

   public static JsonObject CreateToolBudgetTraceEntry(
      int turn,
      int? maxToolRounds,
      int toolRoundCount,
      int payloadCharacterCount,
      decimal? temperature,
      IReadOnlyList<LlamaConditionalTool>? conditionalTools = null
   )
   {
      var conditionalToolTrace = new JsonArray();

      if(conditionalTools is not null)
      {
         foreach(var tool in conditionalTools)
         {
            conditionalToolTrace.Add(
               new JsonObject
               {
                  ["name"] = tool.Name,
                  ["behavior"] = tool.Behavior
               }
            );
         }
      }

      var hasConditionalTools = conditionalToolTrace.Count > 0;

      if(maxToolRounds is null)
      {
         return new JsonObject
         {
            ["kind"] = "budget",
            ["turn"] = turn,
            ["enabled"] = false,
            ["temperature"] = temperature,
            ["conditional_tools"] = hasConditionalTools
               ? conditionalToolTrace
               : null
         };
      }

      var remainingToolCalls = Math.Max(maxToolRounds.Value - toolRoundCount,
         0);

      return new JsonObject
      {
         ["kind"] = "budget",
         ["turn"] = turn,
         ["enabled"] = true,
         ["remaining"] = remainingToolCalls,
         ["max"] = maxToolRounds.Value,
         ["payload_chars"] = payloadCharacterCount,
         ["temperature"] = temperature,
         ["conditional_tools"] = hasConditionalTools
            ? conditionalToolTrace
            : null,
         ["content"] = $"Tool calls remaining: {remainingToolCalls} of " +
            $"{maxToolRounds.Value}."
      };
   }

   public static JsonObject CreateToolSubmissionTraceEntry(
      int turn,
      LlamaToolCall submission
   )
   {
      return new JsonObject
      {
         ["kind"] = "submission",
         ["turn"] = turn,
         ["tool_call_id"] = submission.Id,
         ["name"] = submission.Name,
         ["arguments"] = submission.Arguments
      };
   }

   public static JsonObject CreateToolTraceEntry(
      int turn,
      LlamaToolCall toolCall,
      string toolResult,
      string? searchProvider = null,
      string? searchProviderDetails = null,
      string? searchEngine = null,
      string? pageFetcher = null
   )
   {
      var isSearchTool = string.Equals(
         toolCall.Name,
         WebToolNames.Search,
         StringComparison.Ordinal
      );

      var isGetPageTool = string.Equals(
         toolCall.Name,
         WebToolNames.GetPage,
         StringComparison.Ordinal
      );
      var isFindInPageTool = string.Equals(
         toolCall.Name,
         WebToolNames.FindInPage,
         StringComparison.Ordinal
      );
      var find = LlamaToolArguments.ExtractFind(toolCall.Arguments);

      return new JsonObject
      {
         ["kind"] = "tool",
         ["turn"] = turn,
         ["tool_call_id"] = toolCall.Id,
         ["name"] = toolCall.Name,
         ["arguments"] = toolCall.Arguments,
         ["query"] = isSearchTool
            ? LlamaToolArguments.ExtractQuery(toolCall.Arguments)
            : null,
         ["limit"] = isSearchTool
            ? LlamaToolArguments.ExtractLimit(toolCall.Arguments)
            : null,
         ["url"] = isGetPageTool || isFindInPageTool
            ? LlamaToolArguments.ExtractUrl(toolCall.Arguments)
            : null,
         ["find"] = isFindInPageTool || !string.IsNullOrWhiteSpace(find)
            ? find
            : null,
         ["search_provider"] = isSearchTool ? searchProvider : null,
         ["search_provider_details"] = isSearchTool
            ? searchProviderDetails
            : null,
         ["search_engine"] = isSearchTool ? searchEngine : null,
         ["fetcher"] = isGetPageTool || isFindInPageTool
            ? pageFetcher
            : null,
         ["result"] = toolResult
      };
   }

   public static JsonObject CreateToolFormatFallbackTraceEntry(
      int turn,
      string reason,
      bool willContinueWithTools
   )
   {
      var preview = reason.ReplaceLineEndings(" ").Trim();

      if(preview.Length > LlamaServerDefaults.PreviewSnippetCharacters)
      {
         preview = preview[..LlamaServerDefaults.PreviewSnippetCharacters] +
            "...";
      }

      var content =
         "Tool request failed in llama-server structured output parsing. " +
         (willContinueWithTools
            ? "Retrying with tools."
            : "Continuing without tools.");

      return new JsonObject
      {
         ["kind"] = "tool_format_fallback",
         ["turn"] = turn,
         ["reason"] = preview,
         ["content"] = content
      };
   }

   public static JsonObject CreateValidationFeedbackTraceEntry(
      int turn,
      string validationError,
      bool toolsRemain
   )
   {
      var preview = validationError.ReplaceLineEndings(" ").Trim();

      if(preview.Length > LlamaServerDefaults.PreviewSnippetCharacters)
      {
         preview = preview[..LlamaServerDefaults.PreviewSnippetCharacters] +
            "...";
      }

      return new JsonObject
      {
         ["kind"] = "validation_feedback",
         ["turn"] = turn,
         ["validation_error"] = preview,
         ["content"] = toolsRemain
            ? "Final answer rejected while tools remained. Continuing " +
               "tool loop with validation feedback."
            : "Final answer rejected after the tool budget was exhausted. " +
               "Retrying the final answer with validation feedback."
      };
   }

   public static async Task ReportProgressAsync(
      JsonArray toolTrace,
      int toolRoundCount,
      JsonSerializerOptions jsonOptions,
      Func<string?, int, CancellationToken, Task>? toolTraceUpdated,
      CancellationToken cancellationToken
   )
   {
      if(toolTraceUpdated is null)
      {
         return;
      }

      var toolTraceJson = toolTrace.Count == 0
         ? null
         : JsonSerializer.Serialize(toolTrace, jsonOptions);

      await toolTraceUpdated(toolTraceJson, toolRoundCount, cancellationToken);
   }
}
