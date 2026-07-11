using SESport.AI.Clients;
using SESport.Core.AI;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.AI.Llama;

internal static class LlamaRequestFactory
{
   private const string StructuredOutputPromptMarker =
      "Output format instructions:";

   public static JsonObject CreateInitial(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt,
      bool includeTools
   )
   {
      var payload = CreateBaseRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt,
         includeTools
      );

      if(includeTools)
      {
         payload["tools"] = CreateToolsArray(job.ToolsJson);
         payload["tool_choice"] = "required";
      }

      MergeRequestOptions(payload, provider.RequestOptionsJson);
      MergeRequestOptions(payload, prompt.RequestOptionsJson);
      if(includeTools)
      {
         RemoveStructuredResponseFormat(payload);
      }

      return payload;
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
         TruncateValidationError(validationError) +
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

   private static string TruncateValidationError(string validationError)
   {
      var preview = validationError.ReplaceLineEndings(" ").Trim();

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

   private static JsonArray CreateToolsArray(string? toolsJson)
   {
      if(string.IsNullOrWhiteSpace(toolsJson))
      {
         throw new InvalidOperationException(
            "Tool usage is enabled but no tools JSON was configured."
         );
      }

      var tools = JsonNode.Parse(toolsJson) as JsonArray;

      if(tools is null)
      {
         throw new InvalidOperationException(
            "Configured tools JSON must be a JSON array."
         );
      }

      return tools;
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
