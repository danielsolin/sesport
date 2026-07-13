using System.Text.Json.Nodes;

namespace SESport.AI.Llama;

internal static class LlamaReportSubmission
{
   public const string ToolName = "submit_report";

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
