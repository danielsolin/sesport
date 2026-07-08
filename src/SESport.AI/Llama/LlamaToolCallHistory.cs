namespace SESport.AI.Llama;

internal static class LlamaToolCallHistory
{
   public static bool TryGetRepeatedToolResult(
      LlamaToolCall toolCall,
      IReadOnlyDictionary<string, LlamaToolCallRecord> history,
      out string repeatedResult
   )
   {
      repeatedResult = "";
      var signature = BuildToolCallSignature(toolCall);

      if(string.IsNullOrWhiteSpace(signature) ||
         !history.TryGetValue(signature, out var record))
      {
         return false;
      }

      repeatedResult = CreateRepeatedToolReplayMessage(
         toolCall.Name,
         record.Result
      );
      return true;
   }

   public static string CreateRepeatedToolResultMessage(string toolName)
   {
      return $"Repeated {toolName} call detected. No new information.";
   }

   public static string CreateRepeatedToolReplayMessage(
      string toolName,
      string cachedResult
   )
   {
      var result = new System.Text.StringBuilder();

      result.AppendLine(CreateRepeatedToolResultMessage(toolName));

      if(!string.IsNullOrWhiteSpace(cachedResult))
      {
         result.AppendLine();
         result.Append(cachedResult);
      }

      return result.ToString().TrimEnd();
   }

   public static void RecordToolCallResult(
      LlamaToolCall toolCall,
      IDictionary<string, LlamaToolCallRecord> history,
      int turn,
      string result
   )
   {
      var signature = BuildToolCallSignature(toolCall);

      if(string.IsNullOrWhiteSpace(signature))
      {
         return;
      }

      history[signature] = new LlamaToolCallRecord(turn, result);
   }

   public static string BuildPageCallSignature(
      string toolName,
      string url,
      string find
   )
   {
      return $"{toolName}|url={url}|find={find}";
   }

   public static bool TryGetRepeatedResult(
      string signature,
      IReadOnlyDictionary<string, LlamaToolCallRecord> history,
      out string repeatedResult
   )
   {
      repeatedResult = "";

      if(string.IsNullOrWhiteSpace(signature) ||
         !history.TryGetValue(signature, out var record))
      {
         return false;
      }

      repeatedResult = record.Result;
      return true;
   }

   public static void RecordResult(
      string signature,
      IDictionary<string, LlamaToolCallRecord> history,
      int turn,
      string result
   )
   {
      if(string.IsNullOrWhiteSpace(signature))
      {
         return;
      }

      history[signature] = new LlamaToolCallRecord(turn, result);
   }

   public static string BuildToolCallSignature(LlamaToolCall toolCall)
   {
      var query = LlamaToolArguments.ExtractQuery(toolCall.Arguments);
      var limit = LlamaToolArguments.ExtractLimit(toolCall.Arguments);
      var url = LlamaToolArguments.ExtractUrl(toolCall.Arguments);
      var find = LlamaToolArguments.ExtractFind(toolCall.Arguments);

      return toolCall.Name switch
      {
         WebToolNames.Search =>
            $"{toolCall.Name}|query={query}|limit={limit}",
         WebToolNames.GetPage => BuildPageToolCallSignature(
            toolCall.Name,
            url,
            ""
         ),
         WebToolNames.FindInPage => BuildPageToolCallSignature(
            toolCall.Name,
            url,
            find
         ),
         _ => $"{toolCall.Name}|arguments={toolCall.Arguments.Trim()}"
      };
   }

   private static string BuildPageToolCallSignature(
      string toolName,
      string url,
      string find
   )
   {
      return $"{toolName}|url={url}|find={find}";
   }
}
