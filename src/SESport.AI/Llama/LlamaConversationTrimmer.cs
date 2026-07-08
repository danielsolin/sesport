using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.AI.Llama;

internal static class LlamaConversationTrimmer
{
   private const string ConversationHistorySummaryPrefix =
      "Conversation history summary:";

   internal static void TrimMessages(
      JsonObject request,
      JsonArray messages,
      int maxCharacters,
      JsonSerializerOptions jsonOptions
   )
   {
      var historyEntries = GetConversationHistoryEntries(messages);

      while(EstimateRequestPayloadSize(request, jsonOptions) > maxCharacters)
      {
         var lastAssistantIndex = FindLastAssistantMessageIndex(messages);

         if(lastAssistantIndex < 0)
         {
            return;
         }

         var firstAssistantIndex = FindFirstAssistantMessageIndex(messages);

         if(firstAssistantIndex < 0 ||
            firstAssistantIndex >= lastAssistantIndex)
         {
            return;
         }

         var nextAssistantIndex = FindNextAssistantMessageIndex(
            messages,
            firstAssistantIndex + 1,
            lastAssistantIndex
         );

         var removeEndIndex = nextAssistantIndex < 0
            ? lastAssistantIndex
            : nextAssistantIndex;

         var removeCount = removeEndIndex - firstAssistantIndex;

         if(removeCount <= 0)
         {
            return;
         }

         historyEntries.AddRange(SummarizeTrimmedMessages(
            messages,
            firstAssistantIndex,
            removeCount
         ));

         for(var index = 0; index < removeCount; index++)
         {
            messages.RemoveAt(firstAssistantIndex);
         }

         UpdateConversationHistorySummary(messages, historyEntries);
      }

      UpdateConversationHistorySummary(messages, historyEntries);
   }

   internal static int EstimateRequestPayloadSize(
      JsonObject request,
      JsonSerializerOptions jsonOptions
   )
   {
      return request.ToJsonString(jsonOptions).Length;
   }

   internal static string SummarizeToolResult(
      string toolName,
      string toolContent
   )
   {
      if(string.Equals(toolName, WebToolNames.Search, StringComparison.Ordinal))
      {
         return SummarizeSearchToolResult(toolContent);
      }

      if(string.Equals(
         toolName,
         WebToolNames.GetPage,
         StringComparison.Ordinal
      ))
      {
         return SummarizePageToolResult(toolContent);
      }

      if(string.Equals(
         toolName,
         WebToolNames.FindInPage,
         StringComparison.Ordinal
      ))
      {
         return SummarizeFindInPageResult(toolContent);
      }

      return TruncateForSummary(toolContent);
   }

   private static int FindFirstAssistantMessageIndex(JsonArray messages)
   {
      for(var index = 2; index < messages.Count; index++)
      {
         if(IsMessageRole(messages[index], "assistant"))
         {
            return index;
         }
      }

      return -1;
   }

   private static int FindLastAssistantMessageIndex(JsonArray messages)
   {
      for(var index = messages.Count - 1; index >= 2; index--)
      {
         if(IsMessageRole(messages[index], "assistant"))
         {
            return index;
         }
      }

      return -1;
   }

   private static int FindNextAssistantMessageIndex(
      JsonArray messages,
      int startIndex,
      int stopIndexExclusive
   )
   {
      for(var index = startIndex;
         index < messages.Count && index < stopIndexExclusive;
         index++)
      {
         if(IsMessageRole(messages[index], "assistant"))
         {
            return index;
         }
      }

      return -1;
   }

   internal static int FindPrimarySystemMessageIndex(JsonArray messages)
   {
      for(var index = 0; index < messages.Count; index++)
      {
         if(messages[index] is not JsonObject message)
         {
            continue;
         }

         if(!IsMessageRole(message, "system"))
         {
            continue;
         }

         if(IsConversationHistorySummaryMessage(message))
         {
            continue;
         }

         return index;
      }

      return -1;
   }

   private static int FindConversationHistorySummaryMessageIndex(
      JsonArray messages
   )
   {
      for(var index = 0; index < messages.Count; index++)
      {
         if(messages[index] is not JsonObject message)
         {
            continue;
         }

         if(IsConversationHistorySummaryMessage(message))
         {
            return index;
         }
      }

      return -1;
   }

   private static bool IsConversationHistorySummaryMessage(JsonObject message)
   {
      if(!IsMessageRole(message, "system"))
      {
         return false;
      }

      var content = message["content"]?.GetValue<string>() ?? "";
      return content.StartsWith(
         ConversationHistorySummaryPrefix,
         StringComparison.Ordinal
      );
   }

   private static bool IsMessageRole(JsonNode? node, string role)
   {
      return node is JsonObject message &&
         IsMessageRole(message, role);
   }

   private static bool IsMessageRole(JsonObject message, string role)
   {
      return string.Equals(
         message["role"]?.GetValue<string>(),
         role,
         StringComparison.Ordinal
      );
   }

   private static List<string> GetConversationHistoryEntries(
      JsonArray messages
   )
   {
      var summaryIndex = FindConversationHistorySummaryMessageIndex(messages);

      if(summaryIndex < 0 || messages[summaryIndex] is not JsonObject message)
      {
         return [];
      }

      var content = message["content"]?.GetValue<string>() ?? "";
      return ParseConversationHistoryEntries(content);
   }

   private static void UpdateConversationHistorySummary(
      JsonArray messages,
      IReadOnlyList<string> entries
   )
   {
      var summaryIndex = FindConversationHistorySummaryMessageIndex(messages);

      if(entries.Count == 0)
      {
         if(summaryIndex >= 0)
         {
            messages.RemoveAt(summaryIndex);
         }

         return;
      }

      var summaryMessage = new JsonObject
      {
         ["role"] = "system",
         ["content"] = BuildConversationHistorySummary(entries)
      };

      if(summaryIndex >= 0)
      {
         messages[summaryIndex] = summaryMessage;
         return;
      }

      var insertionIndex = FindPrimarySystemMessageIndex(messages);
      if(insertionIndex < 0)
      {
         messages.Insert(0, summaryMessage);
         return;
      }

      messages.Insert(insertionIndex + 1, summaryMessage);
   }

   private static string BuildConversationHistorySummary(
      IReadOnlyList<string> entries
   )
   {
      var builder = new StringBuilder();

      builder.AppendLine(ConversationHistorySummaryPrefix);

      foreach(var entry in entries)
      {
         builder.AppendLine($"- {entry}");
      }

      return builder.ToString().TrimEnd();
   }

   private static List<string> ParseConversationHistoryEntries(string content)
   {
      if(string.IsNullOrWhiteSpace(content) ||
         !content.StartsWith(
            ConversationHistorySummaryPrefix,
            StringComparison.Ordinal
         ))
      {
         return [];
      }

      var entries = new List<string>();
      var lines = content.ReplaceLineEndings("\n").Split('\n');

      foreach(var line in lines.Skip(1))
      {
         var entry = line.Trim();

         if(string.IsNullOrWhiteSpace(entry))
         {
            continue;
         }

         if(entry.StartsWith("- ", StringComparison.Ordinal))
         {
            entry = entry[2..].Trim();
         }

         if(!string.IsNullOrWhiteSpace(entry))
         {
            entries.Add(entry);
         }
      }

      return entries;
   }

   private static List<string> SummarizeTrimmedMessages(
      JsonArray messages,
      int startIndex,
      int removeCount
   )
   {
      var entries = new List<string>();
      var endIndex = startIndex + removeCount;
      IReadOnlyList<TrimmedToolCall> currentToolCalls = [];
      var currentToolIndex = 0;

      for(var index = startIndex; index < endIndex; index++)
      {
         if(messages[index] is not JsonObject message)
         {
            continue;
         }

         var role = message["role"]?.GetValue<string>() ?? "";

         if(string.Equals(role, "assistant", StringComparison.Ordinal))
         {
            currentToolCalls = ParseMessageToolCalls(message);
            currentToolIndex = 0;

            if(currentToolCalls.Count > 0)
            {
               entries.Add(
                  $"assistant: requested {SummarizeToolCalls(currentToolCalls)}"
               );
            }

            var assistantContent = message["content"]?.GetValue<string>() ??
               "";

            if(currentToolCalls.Count == 0 &&
               !string.IsNullOrWhiteSpace(assistantContent))
            {
               entries.Add(
                  $"assistant: {TruncateForSummary(assistantContent)}"
               );
            }

            continue;
         }

         if(!string.Equals(role, "tool", StringComparison.Ordinal))
         {
            continue;
         }

         var toolName = message["name"]?.GetValue<string>() ?? "";
         var toolContent = message["content"]?.GetValue<string>() ?? "";

         if(currentToolIndex < currentToolCalls.Count)
         {
            var toolCall = currentToolCalls[currentToolIndex];
            entries.Add(
               $"{FormatConversationToolCall(toolCall)} -> " +
               $"{SummarizeToolResult(toolCall.Name, toolContent)}"
            );
            currentToolIndex++;
            continue;
         }

         entries.Add(
            $"{toolName}: {SummarizeToolResult(toolName, toolContent)}"
         );
      }

      return entries;
   }

   private static string SummarizeToolCalls(
      IReadOnlyList<TrimmedToolCall> toolCalls
   )
   {
      var summary = string.Join(
         "; ",
         toolCalls.Select(FormatConversationToolCall)
      );

      return TruncateForSummary(summary, 240);
   }

   private static string SummarizeSearchToolResult(string toolContent)
   {
      try
      {
         using var document = JsonDocument.Parse(toolContent);

         if(document.RootElement.ValueKind != JsonValueKind.Array)
         {
            return TruncateForSummary(toolContent);
         }

         var results = document.RootElement.EnumerateArray().ToArray();
         if(results.Length == 0)
         {
            return "0 results";
         }

         var firstResult = results[0];
         var firstTitle = GetJsonString(firstResult, "title") ?? "";
         var firstUrl = GetJsonString(firstResult, "url") ?? "";

         return results.Length == 1
            ? $"1 result: {firstTitle} ({firstUrl})"
            : $"{results.Length} results: {firstTitle} ({firstUrl})";
      }
      catch(JsonException)
      {
         return "search results fetched";
      }
   }

   private static string SummarizePageToolResult(string toolContent)
   {
      var title = GetLineValue(toolContent, "Title:");
      var url = GetLineValue(toolContent, "URL:");
      var fetchError = GetLineValue(toolContent, "Fetch error:");

      if(!string.IsNullOrWhiteSpace(fetchError))
      {
         return string.IsNullOrWhiteSpace(url)
            ? $"fetch error: {TruncateForSummary(fetchError, 120)}"
            : $"fetch error for {url}: {TruncateForSummary(fetchError, 120)}";
      }

      if(string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(url))
      {
         return "page fetched";
      }

      if(string.IsNullOrWhiteSpace(title))
      {
         return $"page: {url}";
      }

      return string.IsNullOrWhiteSpace(url)
         ? $"page title: {title}"
         : $"page title: {title} ({url})";
   }

   private static string SummarizeFindInPageResult(string toolContent)
   {
      try
      {
         using var document = JsonDocument.Parse(toolContent);

         if(document.RootElement.ValueKind != JsonValueKind.Object)
         {
            return TruncateForSummary(toolContent);
         }

         var title = GetJsonString(document.RootElement, "title") ?? "";
         var url = GetJsonString(document.RootElement, "url") ?? "";
         var matchCount = GetJsonInt32(
            document.RootElement,
            "match_count"
         );

         if(matchCount is null)
         {
            return TruncateForSummary(toolContent);
         }

         if(string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(url))
         {
            return $"{matchCount} matches";
         }

         return string.IsNullOrWhiteSpace(url)
            ? $"{matchCount} matches in {title}"
            : $"{matchCount} matches in {title} ({url})";
      }
      catch(JsonException)
      {
         return "page matches fetched";
      }
   }

   private static string? GetLineValue(string value, string prefix)
   {
      var lines = value.ReplaceLineEndings("\n").Split('\n');

      foreach(var line in lines)
      {
         var trimmedLine = line.TrimStart();

         if(!trimmedLine.StartsWith(prefix, StringComparison.Ordinal))
         {
            continue;
         }

         return trimmedLine[prefix.Length..].Trim();
      }

      return null;
   }

   private static string? GetJsonString(
      JsonElement element,
      string propertyName
   )
   {
      if(!element.TryGetProperty(propertyName, out var property))
      {
         return null;
      }

      return property.ValueKind == JsonValueKind.String
         ? property.GetString()
         : property.ToString();
   }

   private static int? GetJsonInt32(
      JsonElement element,
      string propertyName
   )
   {
      if(!element.TryGetProperty(propertyName, out var property))
      {
         return null;
      }

      return property.ValueKind == JsonValueKind.Number &&
         property.TryGetInt32(out var value)
         ? value
         : null;
   }

   private static IReadOnlyList<TrimmedToolCall> ParseMessageToolCalls(
      JsonObject message
   )
   {
      if(!message.TryGetPropertyValue("tool_calls", out var toolCallsNode) ||
         toolCallsNode is not JsonArray toolCallsArray ||
         toolCallsArray.Count == 0)
      {
         return [];
      }

      var toolCalls = new List<TrimmedToolCall>();

      foreach(var toolCallNode in toolCallsArray)
      {
         if(toolCallNode is not JsonObject toolCallObject)
         {
            continue;
         }

         var id = toolCallObject["id"]?.GetValue<string>() ?? "";
         var function = toolCallObject["function"] as JsonObject;
         var name = function?["name"]?.GetValue<string>() ?? "";
         var arguments = function?["arguments"] is JsonValue value &&
            value.TryGetValue<string>(out var stringArguments)
            ? stringArguments
            : function?["arguments"]?.ToJsonString() ?? "";

         if(string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
         {
            continue;
         }

         toolCalls.Add(new TrimmedToolCall(name, arguments));
      }

      return toolCalls;
   }

   private static string FormatConversationToolCall(
      TrimmedToolCall toolCall
   )
   {
      var query = ExtractStringArgument(toolCall.Arguments, "query");
      var limit = ExtractLimitArgument(toolCall.Arguments);
      var url = ExtractStringArgument(toolCall.Arguments, "url");
      var find = ExtractStringArgument(toolCall.Arguments, "find");

      return toolCall.Name switch
      {
         WebToolNames.Search =>
            $"{toolCall.Name}(query={FormatSummaryQuoted(query)}, " +
            $"limit={limit})",
         WebToolNames.GetPage =>
            FormatConversationPageToolCall(toolCall.Name, url, ""),
         WebToolNames.FindInPage =>
            FormatConversationPageToolCall(toolCall.Name, url, find),
         _ => $"{toolCall.Name}({TruncateForSummary(toolCall.Arguments)})"
      };
   }

   private static string FormatConversationPageToolCall(
      string toolName,
      string url,
      string find
   )
   {
      var parts = new List<string>();

      if(!string.IsNullOrWhiteSpace(url))
      {
         parts.Add($"url={FormatSummaryQuoted(url)}");
      }

      if(!string.IsNullOrWhiteSpace(find))
      {
         parts.Add($"find={FormatSummaryQuoted(find)}");
      }

      return $"{toolName}({string.Join(", ", parts)})";
   }

   private static string ExtractStringArgument(
      string arguments,
      string propertyName
   )
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return "";
      }

      try
      {
         using var document = JsonDocument.Parse(arguments);
         var root = document.RootElement;

         if(root.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
         {
            return property.GetString() ?? "";
         }
      }
      catch(JsonException)
      {
      }

      return string.Equals(
         propertyName,
         "query",
         StringComparison.Ordinal
      )
         ? arguments.Trim()
         : "";
   }

   private static int ExtractLimitArgument(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return 10;
      }

      try
      {
         using var document = JsonDocument.Parse(arguments);
         var root = document.RootElement;

         if(root.TryGetProperty("limit", out var maxResultsNode) &&
            maxResultsNode.ValueKind == JsonValueKind.Number &&
            maxResultsNode.TryGetInt32(out var maxResults))
         {
            return Math.Clamp(maxResults, 1, 10);
         }
      }
      catch(JsonException)
      {
      }

      return 10;
   }

   private static string FormatSummaryQuoted(string value)
   {
      return "'" + value.Replace("'", "\\'", StringComparison.Ordinal) + "'";
   }

   private static string TruncateForSummary(
      string value,
      int maxLength = 220
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return "";
      }

      var normalized = value.ReplaceLineEndings(" ").Trim();

      if(normalized.Length <= maxLength)
      {
         return normalized;
      }

      return normalized[..maxLength] + "...";
   }

   private sealed record TrimmedToolCall(
      string Name,
      string Arguments
   );
}
