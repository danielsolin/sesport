using System.Text.Json.Nodes;

using SESport.Core.AI;

namespace SESport.AI.Llama;

internal static class LlamaStructuredOutputRepair
{
   public static bool CanRepair(
      string outputMode,
      AiPromptDefinition prompt
   )
   {
      return !string.IsNullOrWhiteSpace(prompt.OutputSchemaJson) ||
         string.Equals(
            outputMode,
            "json_object",
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
      return exception.Message.Contains(
         "invalid json_schema output",
         StringComparison.OrdinalIgnoreCase
      ) || exception.Message.Contains(
         "invalid json_object output",
         StringComparison.OrdinalIgnoreCase
      );
   }

   public static bool ShouldRepair(
      string stage,
      string outputMode,
      AiPromptDefinition prompt
   )
   {
      return string.Equals(stage, "final", StringComparison.Ordinal) &&
         CanRepair(outputMode, prompt);
   }

   public static void ApplyRepairPrompt(JsonArray messages)
   {
      var repairPrompt = GetRepairPrompt();

      var repairMessage = new JsonObject
      {
         ["role"] = "system",
         ["content"] = repairPrompt
      };

      var insertionIndex =
         LlamaConversationTrimmer.FindPrimarySystemMessageIndex(messages);

      if(insertionIndex < 0)
      {
         messages.Insert(0, repairMessage);
         return;
      }

      messages.Insert(insertionIndex + 1, repairMessage);
   }

   public static JsonObject CreateRepairPromptTraceEntry(int turn)
   {
      return new JsonObject
      {
         ["kind"] = "repair_prompt",
         ["turn"] = turn,
         ["content"] = GetRepairPrompt()
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
