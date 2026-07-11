using System.Text.Json.Nodes;

namespace SESport.AI.Llama;

internal static class LlamaReportSubmission
{
   private const int MinimumParticipantCount = 1;
   public const string ToolName = "submit_report";

   public static void AddTool(
      JsonObject request,
      string? outputSchemaJson
   )
   {
      if(string.IsNullOrWhiteSpace(outputSchemaJson) ||
         request["tools"] is not JsonArray tools ||
         tools.OfType<JsonObject>().Any(IsReportTool))
      {
         return;
      }

      var parameters = JsonNode.Parse(outputSchemaJson) as JsonObject;

      if(parameters is null)
      {
         return;
      }

      RequireParticipant(parameters);

      tools.Add(
         new JsonObject
         {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
               ["name"] = ToolName,
               ["description"] =
                  "Submit the complete final report when research has " +
                  "identified at least one supported participant. Use web " +
                  "tools instead if no participant has been identified.",
               ["parameters"] = parameters
            }
         }
      );
   }

   private static void RequireParticipant(JsonObject parameters)
   {
      if(parameters["properties"] is not JsonObject properties ||
         properties["Participants"] is not JsonObject participants)
      {
         return;
      }

      participants["minItems"] = MinimumParticipantCount;
   }

   public static void RemoveTool(JsonObject request)
   {
      if(request["tools"] is not JsonArray tools)
      {
         return;
      }

      for(var index = tools.Count - 1; index >= 0; index--)
      {
         if(tools[index] is JsonObject tool && IsReportTool(tool))
         {
            tools.RemoveAt(index);
         }
      }
   }

   public static bool TryGetSubmission(
      IReadOnlyList<LlamaToolCall> toolCalls,
      out LlamaToolCall submission
   )
   {
      submission = toolCalls.FirstOrDefault(toolCall => string.Equals(
         toolCall.Name,
         ToolName,
         StringComparison.Ordinal
      )) ?? new LlamaToolCall("", "", "");

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

   private static bool IsReportTool(JsonObject tool)
   {
      return tool["function"] is JsonObject function &&
         string.Equals(
            function["name"]?.GetValue<string>(),
            ToolName,
            StringComparison.Ordinal
         );
   }
}
