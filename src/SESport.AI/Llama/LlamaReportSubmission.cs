using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.AI.Llama;

internal static class LlamaReportSubmission
{
   public const string ToolName = "submit_report";

   private const string CorruptedParticipantNamePrompt =
      "The previous report contains a corrupted participant name. " +
      "Retry the same report once with the same evidence and a clean name.";

   public static void RemoveTool(JsonObject request, string toolName)
   {
      if(request["tools"] is not JsonArray tools)
      {
         return;
      }

      for(var index = tools.Count - 1; index >= 0; index--)
      {
         if(tools[index] is JsonObject tool &&
            IsNamedTool(tool, toolName))
         {
            tools.RemoveAt(index);
         }
      }
   }

   public static bool TryGetSubmission(
      IReadOnlyList<LlamaToolCall> toolCalls,
      ISet<string> submissionToolNames,
      out LlamaToolCall submission
   )
   {
      submission = toolCalls.FirstOrDefault(toolCall =>
         submissionToolNames.Contains(toolCall.Name)
      ) ?? new LlamaToolCall("", "", "");

      return !string.IsNullOrWhiteSpace(submission.Id);
   }

   public static JsonObject CreateFinalResponse(
      JsonObject response,
      LlamaToolCall submission
   )
   {
      var finalResponse = (JsonObject)response.DeepClone();

      if(finalResponse["choices"] is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         choice["message"] is not JsonObject message)
      {
         throw new InvalidOperationException(
            "submit_report response did not contain an assistant message."
         );
      }

      message["content"] = submission.Arguments;
      message.Remove("tool_calls");
      choice["finish_reason"] = "stop";
      return finalResponse;
   }

   public static bool TryGetCorruptedParticipantNameReason(
      string submissionArguments,
      out string validationError
   )
   {
      validationError = "";

      if(string.IsNullOrWhiteSpace(submissionArguments))
      {
         return false;
      }

      try
      {
         using var document = JsonDocument.Parse(submissionArguments);

         if(document.RootElement.ValueKind != JsonValueKind.Object)
         {
            return false;
         }

         if(!TryGetArrayProperty(
            document.RootElement,
            "Participants",
            out var participants
         ))
         {
            return false;
         }

         foreach(var participant in participants.EnumerateArray())
         {
            var participantName = ReadParticipantName(participant);

            if(IsSuspiciousParticipantName(participantName))
            {
               validationError =
                  "The previous report contains a corrupted participant " +
                  "name.";
               return true;
            }
         }
      }
      catch(JsonException)
      {
         return false;
      }

      return false;
   }

   public static string GetCorruptedParticipantNamePrompt()
   {
      return CorruptedParticipantNamePrompt;
   }

   private static bool TryGetArrayProperty(
      JsonElement root,
      string propertyName,
      out JsonElement value
   )
   {
      if(root.TryGetProperty(propertyName, out value) &&
         value.ValueKind == JsonValueKind.Array)
      {
         return true;
      }

      value = default;
      return false;
   }

   private static string? ReadParticipantName(JsonElement participant)
   {
      if(participant.ValueKind == JsonValueKind.String)
      {
         return participant.GetString();
      }

      if(participant.ValueKind == JsonValueKind.Object &&
         participant.TryGetProperty("Name", out var name) &&
         name.ValueKind == JsonValueKind.String)
      {
         return name.GetString();
      }

      return null;
   }

   private static bool IsSuspiciousParticipantName(string? participantName)
   {
      if(string.IsNullOrWhiteSpace(participantName))
      {
         return true;
      }

      var trimmed = participantName.Trim();

      return trimmed.Contains('?') ||
         trimmed.Contains("...") ||
         trimmed.Contains('…') ||
         trimmed.Contains('�');
   }

   private static bool IsNamedTool(JsonObject tool, string toolName)
   {
      return tool["function"] is JsonObject function &&
         string.Equals(
            function["name"]?.GetValue<string>(),
            toolName,
            StringComparison.Ordinal
         );
   }
}
