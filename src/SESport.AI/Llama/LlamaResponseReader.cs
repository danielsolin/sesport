using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.AI.Llama;

internal static class LlamaResponseReader
{
   public static bool TryGetToolCalls(
      JsonObject response,
      out IReadOnlyList<LlamaToolCall> toolCalls
   )
   {
      return TryReadToolCalls(response, out toolCalls);
   }

   public static string? GetFinishReason(JsonObject response)
   {
      if(!response.TryGetPropertyValue("choices", out var choicesNode) ||
         choicesNode is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         !choice.TryGetPropertyValue("finish_reason", out var reasonNode))
      {
         return null;
      }

      return reasonNode is JsonValue value &&
         value.TryGetValue<string>(out var finishReason)
         ? finishReason
         : reasonNode is null
            ? ""
            : reasonNode.ToJsonString();
   }

   public static string ExtractReasoningContent(JsonObject response)
   {
      if(!response.TryGetPropertyValue("choices", out var choicesNode) ||
         choicesNode is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         !choice.TryGetPropertyValue("message", out var messageNode) ||
         messageNode is not JsonObject message ||
         !message.TryGetPropertyValue("reasoning_content",
            out var reasoningNode))
      {
         return "";
      }

      return reasoningNode is JsonValue value &&
         value.TryGetValue<string>(out var reasoningContent)
         ? reasoningContent
         : reasoningNode is null
            ? ""
            : reasoningNode.ToJsonString();
   }

   public static string[] ExtractToolCallNames(JsonObject response)
   {
      if(!TryGetToolCalls(response, out var toolCalls))
      {
         return [];
      }

      return toolCalls.Select(toolCall => toolCall.Name).ToArray();
   }

   public static void AppendAssistantMessage(
      JsonArray messages,
      JsonObject response,
      JsonSerializerOptions jsonOptions
   )
   {
      messages.Add(CreateAssistantMessage(response, jsonOptions));
   }

   public static JsonObject CreateAssistantMessage(
      JsonObject response,
      JsonSerializerOptions jsonOptions
   )
   {
      var hasToolCalls = TryReadToolCalls(response, out var toolCalls);
      var assistantMessage = new JsonObject
      {
         ["role"] = "assistant",
         ["content"] = hasToolCalls
            ? ""
            : ExtractMessageContent(response)
      };

      if(hasToolCalls)
      {
         assistantMessage["tool_calls"] = JsonSerializer.SerializeToNode(
            toolCalls.Select(toolCall => new
            {
               id = toolCall.Id,
               type = "function",
               function = new
               {
                  name = toolCall.Name,
                  arguments = toolCall.Arguments
               }
            }).ToArray(),
            jsonOptions
         );
      }

      return assistantMessage;
   }

   public static JsonObject CreateToolMessage(
      string toolCallId,
      string result
   )
   {
      return new JsonObject
      {
         ["role"] = "tool",
         ["tool_call_id"] = toolCallId,
         ["content"] = result
      };
   }

   public static string ExtractFinalText(
      JsonObject response,
      JsonSerializerOptions jsonOptions
   )
   {
      if(
         !response.TryGetPropertyValue("choices", out var choicesNode) ||
         choicesNode is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         !choice.TryGetPropertyValue("message", out var messageNode) ||
         messageNode is not JsonObject message
      )
      {
         return response.ToJsonString(jsonOptions);
      }

      return NormalizeOutput(ExtractMessageContent(message));
   }

   public static string ExtractMessageContent(JsonObject message)
   {
      if(!message.TryGetPropertyValue("content", out var contentNode))
      {
         return "";
      }

      if(contentNode is JsonValue contentValue &&
         contentValue.TryGetValue<string>(out var contentText))
      {
         return contentText;
      }

      if(contentNode is not JsonArray contentArray)
      {
         return contentNode?.ToJsonString() ?? "";
      }

      var builder = new System.Text.StringBuilder();

      foreach(var item in contentArray)
      {
         if(item is JsonObject contentItem &&
            contentItem.TryGetPropertyValue("text", out var textNode) &&
            textNode is JsonValue textValue &&
            textValue.TryGetValue<string>(out var itemText))
         {
            builder.Append(itemText);
         }
      }

      return builder.ToString();
   }

   public static string NormalizeOutput(string value)
   {
      return value
         .Trim()
         .Trim('"', '\'')
         .ReplaceLineEndings(" ");
   }

   private static bool TryReadToolCalls(
      JsonObject response,
      out IReadOnlyList<LlamaToolCall> toolCalls
   )
   {
      toolCalls = [];

      if(
         !response.TryGetPropertyValue("choices", out var choicesNode) ||
         choicesNode is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         !choice.TryGetPropertyValue("message", out var messageNode) ||
         messageNode is not JsonObject message ||
         !message.TryGetPropertyValue("tool_calls", out var toolCallsNode) ||
         toolCallsNode is not JsonArray toolCallsArray ||
         toolCallsArray.Count == 0
      )
      {
         return false;
      }

      var parsedToolCalls = new List<LlamaToolCall>();

      foreach(var toolCallNode in toolCallsArray)
      {
         if(toolCallNode is not JsonObject toolCallObject)
         {
            continue;
         }

         if(!TryReadToolCall(toolCallObject, out var toolCall))
         {
            continue;
         }

         parsedToolCalls.Add(toolCall);
      }

      toolCalls = parsedToolCalls;
      return toolCalls.Count > 0;
   }

   private static bool TryReadToolCall(
      JsonObject toolCallObject,
      out LlamaToolCall toolCall
   )
   {
      toolCall = new LlamaToolCall("", "", "");

      if(
         !toolCallObject.TryGetPropertyValue("id", out var idNode) ||
         idNode is not JsonValue idValue ||
         idValue.TryGetValue<string>(out var id) == false ||
         string.IsNullOrWhiteSpace(id) ||
         !toolCallObject.TryGetPropertyValue(
            "function",
            out var functionNode
         ) ||
         functionNode is not JsonObject functionObject ||
         !functionObject.TryGetPropertyValue("name", out var nameNode) ||
         nameNode is not JsonValue nameValue ||
         nameValue.TryGetValue<string>(out var name) == false ||
         string.IsNullOrWhiteSpace(name)
      )
      {
         return false;
      }

      var arguments = "";

      if(
         functionObject.TryGetPropertyValue(
            "arguments",
            out var argumentsNode
         ) &&
         argumentsNode is not null
      )
      {
         arguments = argumentsNode.ToJsonString();

         if(argumentsNode is JsonValue argumentsValue &&
            argumentsValue.TryGetValue<string>(out var stringArguments))
         {
            arguments = stringArguments;
         }
      }

      toolCall = new LlamaToolCall(id, name, arguments);
      return true;
   }
}
