using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using SESport.AI.Clients;
using SESport.Core.AI;
using SESport.Core.Configuration;

namespace SESport.AI.Llama;

internal sealed record LlamaRequestState(
   JsonObject Request,
   IReadOnlyList<LlamaConditionalTool> ConditionalTools
);

internal static class LlamaRequestFactory
{
   private const string StructuredOutputPromptMarker =
      "Output format instructions:";
   private const string StructuredOutputPromptInstruction =
      StructuredOutputPromptMarker + "\n" +
      "Return only one raw object literal.\n" +
      "The first character must be { and the last character must be }.\n" +
      "Do not use markdown, code fences, commentary, explanations, tool\n" +
      "calls, channel markers, constraint markers, or special tokens.";

   private const string JsonPropertyContent = "content";
   private const string JsonPropertyFunction = "function";
   private const string JsonPropertyGrammar = "grammar";
   private const string JsonPropertyJsonSchema = "json_schema";
   private const string JsonPropertyMessages = "messages";
   private const string JsonPropertyMaxTokens = "max_tokens";
   private const string JsonPropertyModel = "model";
   private const string JsonPropertyName = "name";
   private const string JsonPropertyResponseFormat = "response_format";
   private const string JsonPropertyRole = "role";
   private const string JsonPropertyTemperature = "temperature";
   private const string JsonPropertyToolChoice = "tool_choice";
   private const string JsonPropertyTools = "tools";
   private const string JsonValueJsonObject = "json_object";
   private const string JsonValueRequired = "required";
   private const string JsonValueSystem = "system";
   private const string JsonValueUser = "user";

   private const string ConfiguredToolsJsonMustBeArrayMessage =
      "Configured tools JSON must be a JSON array.";
   private const string FinalAnswerRejectedPrefix =
      "The previous final answer was rejected by schema validation: ";
   private const string FinalReportRejectedPrefix =
      "The previous final report was rejected by schema validation: ";
   private const string NoMoreToolsPrompt =
      "No more tool calls are available. Use only the web research " +
      "already present in this conversation and return the final answer " +
      "now.";
   private const string ObjectShapeDefinitionLabel =
      "Object shape definition:";
   private const string ReportBudgetExhaustedPrompt =
      "The research tool budget is exhausted. Correct the report " +
      "using only evidence already present in this conversation and " +
      "return the complete final answer again. No tool call is " +
      "available.";
   private const string ReportSubmissionAttemptNotice =
      "This happened after a " + WebToolNames.SubmitReport + " attempt.";
   private const string ReportToolsAvailablePrompt =
      "Tools are still available. Continue researching with tool " +
      "calls until the output matches the required schema. Do not " +
      "return another final answer yet.";
   private const string ToolCallsRemainingPrefix =
      "Tool calls remaining: ";
   private const string ToolCallsRemainingSeparator = " of ";
   private const string ToolFormatFeedbackContinuePrompt =
      "Tools are still available. Continue with a valid tool call. " +
      "Do not return a final answer yet.";
   private const string ToolFormatFeedbackPrefix =
      "The previous tool-call attempt could not be parsed by ";
   private const string ToolFormatFeedbackSource = "llama-server: ";
   private const string ToolUsageRequiresConfiguredToolsMessage =
      "Tool usage is enabled but no tools JSON was configured.";
   private const string PromptRequestNamePrefix = "prompt_";

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
            ToolUsageRequiresConfiguredToolsMessage
         );
      }

      if(requestTools.Count > 0)
      {
         payload[JsonPropertyTools] = requestTools;
         payload[JsonPropertyToolChoice] = JsonValueRequired;
      }

      MergeRequestOptions(payload, provider.RequestOptionsJson);
      MergeRequestOptions(payload, prompt.RequestOptionsJson);

      ApplyDefaultMaxTokens(payload);

      if(requestTools.Count > 0)
      {
         RemoveStructuredResponseFormat(payload);
      }

      return new LlamaRequestState(
         payload,
         conditionalTools
      );
   }

   public static JsonObject CreateFinal(
      JsonObject request,
      AiJobDefinition job,
      AiPromptDefinition prompt
   )
   {
      var finalRequest = (JsonObject)request.DeepClone();
      finalRequest.Remove(JsonPropertyTools);
      finalRequest.Remove(JsonPropertyToolChoice);

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

      var remainingToolCalls = Math.Max(
         maxToolRounds.Value - toolRoundCount,
         0
      );
      var budgetPrompt = $"{ToolCallsRemainingPrefix}{remainingToolCalls}" +
         $"{ToolCallsRemainingSeparator}{maxToolRounds.Value}.";
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
      var finalPrompt = NoMoreToolsPrompt;
      var systemPrompt = string.IsNullOrWhiteSpace(baseSystemPrompt)
         ? finalPrompt
         : $"{baseSystemPrompt}{Environment.NewLine}{Environment.NewLine}" +
            finalPrompt;

      UpsertPrimarySystemMessage(messages, systemPrompt);
   }

   public static void AddValidationFeedbackPrompt(
      JsonArray messages,
      string validationError,
      bool reportSubmissionAttempt = false
   )
   {
      var prompt = CreateReportValidationPrompt(
         validationError,
         reportSubmissionAttempt,
         toolsStillAvailable: true
      );

      messages.Add(CreateSystemMessage(prompt));
   }

   public static void AddFinalReportCorrectionPrompt(
      JsonArray messages,
      string validationError,
      bool reportSubmissionAttempt = false
   )
   {
      var prompt = CreateReportValidationPrompt(
         validationError,
         reportSubmissionAttempt,
         toolsStillAvailable: false
      );

      messages.Add(CreateSystemMessage(prompt));
   }

   public static void AddToolFormatFeedbackPrompt(
      JsonArray messages,
      string formatError
   )
   {
      var prompt =
         ToolFormatFeedbackPrefix +
         ToolFormatFeedbackSource +
         TruncateFeedback(formatError) +
         $"{Environment.NewLine}{Environment.NewLine}" +
         ToolFormatFeedbackContinuePrompt;

      messages.Add(CreateSystemMessage(prompt));
   }

   private static void UpsertPrimarySystemMessage(
      JsonArray messages,
      string content
   )
   {
      var systemMessage = CreateSystemMessage(content);

      var systemIndex =
         LlamaConversationTrimmer.FindPrimarySystemMessageIndex(messages);

      if(systemIndex < 0)
      {
         messages.Insert(0, systemMessage);
         return;
      }

      messages[systemIndex] = systemMessage;
   }

   private static JsonObject CreateSystemMessage(string content)
   {
      return CreateMessage(JsonValueSystem, content);
   }

   private static JsonObject CreateMessage(string role, string content)
   {
      return new JsonObject
      {
         [JsonPropertyRole] = role,
         [JsonPropertyContent] = content
      };
   }

   private static string TruncateFeedback(string value)
   {
      var preview = value.ReplaceLineEndings(" ").Trim();

      return preview.Length <= 500 ? preview : preview[..500] + "...";
   }

   private static string CreateReportValidationPrompt(
      string validationError,
      bool reportSubmissionAttempt,
      bool toolsStillAvailable
   )
   {
      var builder = new StringBuilder();

      builder.Append(
         toolsStillAvailable
            ? FinalAnswerRejectedPrefix
            : FinalReportRejectedPrefix
      );
      builder.Append(TruncateFeedback(validationError));
      builder.AppendLine();
      builder.AppendLine();

      if(reportSubmissionAttempt)
      {
         builder.AppendLine(ReportSubmissionAttemptNotice);
         builder.AppendLine();
      }

      if(toolsStillAvailable)
      {
         builder.Append(ReportToolsAvailablePrompt);
      }
      else
      {
         builder.Append(ReportBudgetExhaustedPrompt);
      }

      return builder.ToString();
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
         [JsonPropertyModel] = provider.Model
      };

      payload[JsonPropertyMessages] = CreateMessages(renderedPrompt);

      if(prompt.MaxOutputTokens is not null)
      {
         payload[JsonPropertyMaxTokens] = prompt.MaxOutputTokens.Value;
      }

      if(prompt.Temperature is not null)
      {
         payload[JsonPropertyTemperature] = prompt.Temperature.Value;
      }

      if(!includeTools)
      {
         ApplyStructuredOutputPrompt(payload, job, prompt);
         ApplyStructuredResponseFormat(payload, job, prompt);
      }

      return payload;
   }

   private static void ApplyDefaultMaxTokens(JsonObject payload)
   {
      if(payload.ContainsKey(JsonPropertyMaxTokens))
      {
         return;
      }

      payload[JsonPropertyMaxTokens] = AiDefaults.DefaultMaxOutputTokens;
   }

   private static JsonArray CreateMessages(
      AiRenderedPrompt renderedPrompt
   )
   {
      var messages = new JsonArray();
      var systemPrompt = renderedPrompt.SystemPrompt?.Trim();

      if(!string.IsNullOrWhiteSpace(systemPrompt))
      {
         messages.Add(CreateSystemMessage(systemPrompt));
      }

      messages.Add(
         CreateMessage(
            JsonValueUser,
            renderedPrompt.UserPrompt.Trim()
         )
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

      if(payload[JsonPropertyMessages] is not JsonArray messages)
      {
         return;
      }

      if(HasStructuredOutputPrompt(messages))
      {
         return;
      }

      messages.Add(CreateSystemMessage(CreateStructuredOutputPrompt(prompt)));
   }

   private static bool ShouldRequestStructuredOutput(
      AiJobDefinition job,
      AiPromptDefinition prompt
   )
   {
      return !string.IsNullOrWhiteSpace(prompt.OutputSchemaJson) ||
         string.Equals(
            job.OutputMode,
            JsonValueJsonObject,
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
         $"{PromptRequestNamePrefix}{prompt.Id:N}"
      );
   }

   private static void RemoveStructuredResponseFormat(JsonObject payload)
   {
      payload.Remove(JsonPropertyResponseFormat);
      payload.Remove(JsonPropertyJsonSchema);
      payload.Remove(JsonPropertyGrammar);
   }

   private static bool HasStructuredOutputPrompt(JsonArray messages)
   {
      foreach(var message in messages.OfType<JsonObject>())
      {
         if(message[JsonPropertyContent] is JsonValue value &&
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
      var instruction = StructuredOutputPromptInstruction;

      if(string.IsNullOrWhiteSpace(prompt.OutputSchemaJson))
      {
         return instruction;
      }

      return
         instruction +
         $"{Environment.NewLine}{Environment.NewLine}" +
         ObjectShapeDefinitionLabel +
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
               ConfiguredToolsJsonMustBeArrayMessage
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
      return tool[JsonPropertyFunction] is JsonObject function &&
         function.TryGetPropertyValue(JsonPropertyName, out var name) &&
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
