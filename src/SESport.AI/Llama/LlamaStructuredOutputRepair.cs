using SESport.AI.Protocols;
using SESport.Core.AI;
using System.Text.Json.Nodes;

namespace SESport.AI.Llama;

internal static class LlamaStructuredOutputRepair
{
   private const string InvalidJsonObjectOutputMessage =
      "invalid " + AiOutputModeIds.JsonObject + " output";

   public static bool CanRepair(
      string outputMode,
      AiPromptDefinition prompt
   )
   {
      return !string.IsNullOrWhiteSpace(prompt.OutputSchemaJson) ||
         string.Equals(
            outputMode,
            AiOutputModeIds.JsonObject,
            StringComparison.OrdinalIgnoreCase
         );
   }

   public static bool IsPegNativeFormatFailure(Exception exception)
   {
      return exception.Message.Contains(
         "peg-native format",
         StringComparison.OrdinalIgnoreCase
      );
   }

   public static bool IsToolCallArgumentsParseFailure(Exception exception)
   {
      return exception.Message.Contains(
         "Failed to parse tool call arguments as JSON",
         StringComparison.OrdinalIgnoreCase
      );
   }

   public static bool IsInvalidStructuredOutputFailure(
      Exception exception
   )
   {
      return IsEmptyOutputFailure(exception) ||
         exception.Message.Contains(
         "invalid json_schema output",
         StringComparison.OrdinalIgnoreCase
      ) || exception.Message.Contains(
         InvalidJsonObjectOutputMessage,
         StringComparison.OrdinalIgnoreCase
      );
   }

   public static bool IsEmptyOutputFailure(Exception exception)
   {
      return ResponsesOutputValidator.IsEmptyOutputFailure(exception);
   }

   public static void ApplyRepairPrompt(JsonArray messages)
   {
      ApplyRepairPrompt(messages, GetRepairPrompt());
   }

   public static void ApplyRepairPrompt(
      JsonArray messages,
      string repairPrompt
   )
   {
      var repairMessage = new JsonObject
      {
         ["role"] = "user",
         ["content"] = repairPrompt
      };

      messages.Add(repairMessage);
   }

   public static JsonObject CreateRepairPromptTraceEntry(int turn)
   {
      return CreateRepairPromptTraceEntry(turn, GetRepairPrompt());
   }

   public static JsonObject CreateRepairPromptTraceEntry(
      int turn,
      string repairPrompt
   )
   {
      return new JsonObject
      {
         ["kind"] = "repair_prompt",
         ["turn"] = turn,
         ["content"] = repairPrompt
      };
   }

   public static string GetRepairPrompt()
   {
      return """
         The previous response was rejected because it was not a valid object.
         Return only one raw object literal.
         The first character must be { and the last character must be }.
         Do not use markdown, fences, tool calls, commentary, explanations,
         channel markers, constraint markers, or special tokens.
         """.Trim();
   }
}
