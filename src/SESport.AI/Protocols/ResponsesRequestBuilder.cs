using SESport.Core.AI;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SESport.AI.Protocols;

internal static class ResponsesRequestBuilder
{
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   public static JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   )
   {
      var payload = new JsonObject
      {
         ["model"] = provider.Model,
         ["input"] = renderedPrompt.UserPrompt.Trim()
      };

      if(!string.IsNullOrWhiteSpace(renderedPrompt.SystemPrompt))
      {
         payload["instructions"] = renderedPrompt.SystemPrompt.Trim();
      }

      if(prompt.MaxOutputTokens is not null)
      {
         payload["max_output_tokens"] = prompt.MaxOutputTokens.Value;
      }

      if(prompt.Temperature is not null)
      {
         payload["temperature"] = prompt.Temperature.Value;
      }

      ResponsesRequestFormat.Apply(
         payload,
         job.OutputMode,
         prompt.OutputSchemaJson,
         $"prompt_{prompt.Id:N}"
      );

      MergeRequestOptions(payload, provider.RequestOptionsJson);
      MergeRequestOptions(payload, prompt.RequestOptionsJson);

      if(!payload.ContainsKey("max_output_tokens"))
      {
         payload["max_output_tokens"] = AiDefaults.DefaultMaxOutputTokens;
      }

      return payload;
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
