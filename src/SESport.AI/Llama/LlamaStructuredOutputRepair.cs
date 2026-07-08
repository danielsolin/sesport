using System.Text.Json;
using System.Text.Json.Nodes;

using SESport.AI.Clients;
using SESport.AI.Models;

namespace SESport.AI.Llama;

internal static class LlamaStructuredOutputRepair
{
   public static void ValidateStructuredOutput(
      JsonObject response,
      string outputMode,
      AiPromptDefinition prompt,
      JsonSerializerOptions jsonOptions
   )
   {
      var outputText = LlamaResponseReader.NormalizeOutput(
         LlamaResponseReader.ExtractFinalText(response, jsonOptions)
      );

      ResponsesOutputValidator.ValidateStructuredOutput(
         outputText,
         outputMode,
         prompt.OutputSchemaJson
      );
   }

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
         The previous response was rejected because it was not valid JSON.
         Return only the raw JSON object required by the schema.
         Do not use markdown, fences, tool calls, commentary, or
         explanations.
         """.Trim();
   }
}
