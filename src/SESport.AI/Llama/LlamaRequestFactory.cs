using SESport.AI.Clients;
using SESport.Core.AI;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.AI.Llama;

internal sealed record LlamaRequestState(
   JsonObject Request,
   IReadOnlyList<LlamaConditionalTool> ConditionalTools
);

internal static class LlamaRequestFactory
{
   private const string StructuredOutputPromptMarker =
      "Output format instructions:";

   public static LlamaRequestState CreateInitial(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt,
      bool includeTools
   )
   {
      var conditionalTools = LlamaConditionalTools.Resolve(
         job.ConditionalToolsJson,
         job,
         prompt
      );
      var payload = CreateBaseRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt,
         includeTools
      );

      var requestTools = CreateToolsArray(
         job.ToolsJson,
         conditionalTools
      );

      if(includeTools && requestTools.Count == 0)
      {
         throw new InvalidOperationException(
            "Tool usage is enabled but no tools JSON was configured."
         );
      }

      if(requestTools.Count > 0)
      {
         payload["tools"] = requestTools;
         payload["tool_choice"] = "required";
      }

      MergeRequestOptions(payload, provider.RequestOptionsJson);
      MergeRequestOptions(payload, prompt.RequestOptionsJson);
      if(requestTools.Count > 0)
      {
         RemoveStructuredResponseFormat(payload);
      }

      return new LlamaRequestState(payload, conditionalTools);
   }

   public static JsonObject CreateFinal(
      JsonObject request,
      AiJobDefinition job,
      AiPromptDefinition prompt
   )
   {
      var finalRequest = (JsonObject)request.DeepClone();
      finalRequest.Remove("tools");
      finalRequest.Remove("tool_choice");
      ApplyStructuredOutputPrompt(finalRequest, job, prompt);
      ApplyStructuredResponseFormat(finalRequest, job, prompt);

      return finalRequest;
   }

   public static void ApplyToolBudgetPrompt(
      JsonArray messages,
      string? baseSystemPrompt,
      int? maxToolRounds,
      int toolRoundCount
   )
   {
      if(maxToolRounds is null)
      {
         return;
      }

      var remainingToolCalls = Math.Max(maxToolRounds.Value - toolRoundCount,
         0);
      var budgetPrompt = $"Tool calls remaining: {remainingToolCalls} of " +
         $"{maxToolRounds.Value}.";
      var systemPrompt = string.IsNullOrWhiteSpace(baseSystemPrompt)
         ? budgetPrompt
         : $"{baseSystemPrompt}{Environment.NewLine}{Environment.NewLine}" +
            budgetPrompt;

      UpsertPrimarySystemMessage(messages, systemPrompt);
   }

   public static void ApplyNoMoreToolsPrompt(
      JsonArray messages,
      string? baseSystemPrompt
   )
   {
      var finalPrompt =
         "No more tool calls are available. Use only the web research " +
         "already present in this conversation and return the final answer " +
         "now.";
      var systemPrompt = string.IsNullOrWhiteSpace(baseSystemPrompt)
         ? finalPrompt
         : $"{baseSystemPrompt}{Environment.NewLine}{Environment.NewLine}" +
            finalPrompt;

      UpsertPrimarySystemMessage(messages, systemPrompt);
   }

   public static void AddValidationFeedbackPrompt(
      JsonArray messages,
      string validationError
   )
   {
      var prompt =
         "The previous final answer was rejected by validation: " +
         TruncateFeedback(validationError) +
         $"{Environment.NewLine}{Environment.NewLine}" +
         "Tools are still available. Continue researching with tool calls " +
         "until the validation issue is resolved. Do not return another " +
         "final answer with the same unsupported evidence.";

      messages.Add(
         new JsonObject
         {
            ["role"] = "system",
            ["content"] = prompt
         }
      );
   }

   public static void AddFinalReportCorrectionPrompt(
      JsonArray messages,
      string validationError
   )
   {
      var prompt =
         "The previous final report was rejected by validation: " +
         TruncateFeedback(validationError) +
         $"{Environment.NewLine}{Environment.NewLine}" +
         "The research tool budget is exhausted. Correct the report using " +
         "only evidence already present in this conversation. Preserve all " +
         "participants supported by that evidence, correct the rejected " +
         "fields, and return the complete final answer again. No tool call " +
         "is available.";

      messages.Add(
         new JsonObject
         {
            ["role"] = "system",
            ["content"] = prompt
         }
      );
   }

   public static void AddToolFormatFeedbackPrompt(
      JsonArray messages,
      string formatError
   )
   {
      var prompt =
         "The previous tool-call attempt could not be parsed by " +
         "llama-server: " +
         TruncateFeedback(formatError) +
         $"{Environment.NewLine}{Environment.NewLine}" +
         "Tools are still available. Continue with a valid tool call. " +
         "Do not return a final answer yet.";

      messages.Add(
         new JsonObject
         {
            ["role"] = "system",
            ["content"] = prompt
         }
      );
   }

   private static void UpsertPrimarySystemMessage(
      JsonArray messages,
      string content
   )
   {
      var systemMessage = new JsonObject
      {
         ["role"] = "system",
         ["content"] = content
      };

      var systemIndex =
         LlamaConversationTrimmer.FindPrimarySystemMessageIndex(messages);

      if(systemIndex < 0)
      {
         messages.Insert(0, systemMessage);
         return;
      }

      messages[systemIndex] = systemMessage;
   }

   private static string TruncateFeedback(string value)
   {
      var preview = value.ReplaceLineEndings(" ").Trim();

      return preview.Length <= 500 ? preview : preview[..500] + "...";
   }

   private static JsonObject CreateBaseRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt,
      bool includeTools
   )
   {
      var payload = new JsonObject
      {
         ["model"] = provider.Model
      };

      payload["messages"] = CreateMessages(renderedPrompt);

      if(prompt.MaxOutputTokens is not null)
      {
         payload["max_tokens"] = prompt.MaxOutputTokens.Value;
      }

      if(prompt.Temperature is not null)
      {
         payload["temperature"] = prompt.Temperature.Value;
      }

      if(!includeTools)
      {
         ApplyStructuredOutputPrompt(payload, job, prompt);
         ApplyStructuredResponseFormat(payload, job, prompt);
      }

      return payload;
   }

   private static JsonArray CreateMessages(
      AiRenderedPrompt renderedPrompt
   )
   {
      var messages = new JsonArray();
      var systemPrompt = renderedPrompt.SystemPrompt?.Trim();

      if(!string.IsNullOrWhiteSpace(systemPrompt))
      {
         messages.Add(
            new JsonObject
            {
               ["role"] = "system",
               ["content"] = systemPrompt
            }
         );
      }

      messages.Add(
         new JsonObject
         {
            ["role"] = "user",
            ["content"] = renderedPrompt.UserPrompt.Trim()
         }
      );

      return messages;
   }

   private static void ApplyStructuredOutputPrompt(
      JsonObject payload,
      AiJobDefinition job,
      AiPromptDefinition prompt
   )
   {
      if(!ShouldRequestStructuredOutput(job, prompt))
      {
         return;
      }

      if(payload["messages"] is not JsonArray messages)
      {
         return;
      }

      if(HasStructuredOutputPrompt(messages))
      {
         return;
      }

      messages.Add(
         new JsonObject
         {
            ["role"] = "system",
            ["content"] = CreateStructuredOutputPrompt(prompt)
         }
      );
   }

   private static bool ShouldRequestStructuredOutput(
      AiJobDefinition job,
      AiPromptDefinition prompt
   )
   {
      return !string.IsNullOrWhiteSpace(prompt.OutputSchemaJson) ||
         string.Equals(
            job.OutputMode,
            "json_object",
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static void ApplyStructuredResponseFormat(
      JsonObject payload,
      AiJobDefinition job,
      AiPromptDefinition prompt
   )
   {
      if(!ShouldRequestStructuredOutput(job, prompt))
      {
         return;
      }

      ResponsesRequestFormat.Apply(
         payload,
         job.OutputMode,
         prompt.OutputSchemaJson,
         $"prompt_{prompt.Id:N}"
      );
   }

   private static void RemoveStructuredResponseFormat(JsonObject payload)
   {
      payload.Remove("response_format");
      payload.Remove("json_schema");
      payload.Remove("grammar");
   }

   private static bool HasStructuredOutputPrompt(JsonArray messages)
   {
      foreach(var message in messages.OfType<JsonObject>())
      {
         if(message["content"] is JsonValue value &&
            value.TryGetValue<string>(out var content) &&
            content.Contains(
               StructuredOutputPromptMarker,
               StringComparison.Ordinal
            ))
         {
            return true;
         }
      }

      return false;
   }

   private static string CreateStructuredOutputPrompt(AiPromptDefinition prompt)
   {
      var instruction = """
         Output format instructions:
         Return only one raw object literal.
         The first character must be { and the last character must be }.
         Do not use markdown, code fences, commentary, explanations, tool
         calls, channel markers, constraint markers, or special tokens.
         """.Trim();

      if(string.IsNullOrWhiteSpace(prompt.OutputSchemaJson))
      {
         return instruction;
      }

      return
         instruction +
         $"{Environment.NewLine}{Environment.NewLine}" +
         "Object shape definition:" +
         $"{Environment.NewLine}{prompt.OutputSchemaJson.Trim()}";
   }

   private static JsonArray CreateToolsArray(
      string? toolsJson,
      IReadOnlyList<LlamaConditionalTool> conditionalTools
   )
   {
      var tools = new JsonArray();
      var toolNames = new HashSet<string>(StringComparer.Ordinal);

      if(!string.IsNullOrWhiteSpace(toolsJson))
      {
         var configuredTools = JsonNode.Parse(toolsJson) as JsonArray;

         if(configuredTools is null)
         {
            throw new InvalidOperationException(
               "Configured tools JSON must be a JSON array."
            );
         }

         foreach(var tool in configuredTools.OfType<JsonObject>())
         {
            var toolName = GetToolName(tool);

            if(string.IsNullOrWhiteSpace(toolName) ||
               !toolNames.Add(toolName))
            {
               continue;
            }

            tools.Add(tool.DeepClone());
         }
      }

      foreach(var conditionalTool in conditionalTools)
      {
         if(!toolNames.Add(conditionalTool.Name))
         {
            continue;
         }

         tools.Add(conditionalTool.Tool.DeepClone());
      }

      return tools;
   }

   private static string GetToolName(JsonObject tool)
   {
      return tool["function"] is JsonObject function &&
         function.TryGetPropertyValue("name", out var name) &&
         name is JsonValue jsonValue &&
         jsonValue.TryGetValue<string>(out var text)
         ? text
         : "";
   }

   private static void MergeRequestOptions(
      JsonObject payload,
      string requestOptionsJson
   )
   {
      if(string.IsNullOrWhiteSpace(requestOptionsJson))
      {
         return;
      }

      try
      {
         var requestOptions = JsonNode.Parse(requestOptionsJson) as JsonObject;

         if(requestOptions is null)
         {
            return;
         }

         foreach(var property in requestOptions)
         {
            if(payload.ContainsKey(property.Key))
            {
               continue;
            }

            payload[property.Key] = property.Value?.DeepClone();
         }
      }
      catch(JsonException)
      {
      }
   }
}
