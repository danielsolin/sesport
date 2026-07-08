using System.Text.Json;
using System.Text.Json.Nodes;

using SESport.AI.Clients;
using SESport.AI.Models;

namespace SESport.AI.Llama;

internal static class LlamaRequestFactory
{
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

      ResponsesRequestFormat.Apply(
         finalRequest,
         job.OutputMode,
         prompt.OutputSchemaJson,
         $"prompt_{prompt.Id:N}"
      );

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
      var systemMessage = new JsonObject
      {
         ["role"] = "system",
         ["content"] = systemPrompt
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
         ResponsesRequestFormat.Apply(
            payload,
            job.OutputMode,
            prompt.OutputSchemaJson,
            $"prompt_{prompt.Id:N}"
         );
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
