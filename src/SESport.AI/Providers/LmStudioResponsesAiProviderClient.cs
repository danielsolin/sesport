using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.AI.Validation;

namespace SESport.AI.Providers;

public sealed class LmStudioResponsesAiProviderClient : IAiProviderClient
{
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   public string Kind => "lmstudio";

   public LmStudioResponsesAiProviderClient(HttpClient httpClient)
   {
      HttpClient = httpClient;
   }

   private HttpClient HttpClient { get; }

   public async Task<AiJobResult> GenerateAsync(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      string renderedPrompt,
      string inputPayloadJson,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(provider.BaseAddress))
      {
         throw new InvalidOperationException(
            $"Provider '{provider.Id}' is missing a base address."
         );
      }

      if(string.IsNullOrWhiteSpace(provider.Model))
      {
         throw new InvalidOperationException(
            $"Provider '{provider.Id}' is missing a model."
         );
      }

      var request = CreateRequestPayload(provider, job, prompt, renderedPrompt);
      var response = await SendAsync(provider, request, cancellationToken);
      var rawResponse = await response.Content.ReadAsStringAsync(
         cancellationToken
      );

      if(!response.IsSuccessStatusCode)
      {
         throw new HttpRequestException(
            CreateFailureMessage(response.StatusCode, rawResponse),
            null,
            response.StatusCode
         );
      }

      var outputText = NormalizeOutput(
         ResponsesOutputValidator.ExtractFinalText(rawResponse)
      );

      return new AiJobResult(
         Guid.NewGuid(),
         job.Id,
         provider.Id,
         renderedPrompt,
         outputText,
         rawResponse,
         null
      );
   }

   private JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      string renderedPrompt
   )
   {
      var payload = new JsonObject
      {
         ["model"] = provider.Model,
         ["input"] = renderedPrompt
      };

      if(prompt.MaxOutputTokens is not null)
      {
         payload["max_output_tokens"] = prompt.MaxOutputTokens.Value;
      }

      if(prompt.Temperature is not null)
      {
         payload["temperature"] = prompt.Temperature.Value;
      }

      if(string.Equals(
         job.OutputMode,
         "json_object",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         payload["response_format"] = new JsonObject
         {
            ["type"] = "json_object"
         };
      }
      else if(string.Equals(
         job.OutputMode,
         "json_schema",
         StringComparison.OrdinalIgnoreCase
      ) && !string.IsNullOrWhiteSpace(prompt.OutputSchemaJson))
      {
         payload["response_format"] = new JsonObject
         {
            ["type"] = "json_schema",
            ["json_schema"] = JsonNode.Parse(prompt.OutputSchemaJson)
         };
      }

      MergeRequestOptions(payload, provider.RequestOptionsJson);
      MergeRequestOptions(payload, prompt.RequestOptionsJson);
      return payload;
   }

   private async Task<HttpResponseMessage> SendAsync(
      AiProviderDefinition provider,
      JsonObject request,
      CancellationToken cancellationToken
   )
   {
      var requestMessage = new HttpRequestMessage(
         HttpMethod.Post,
         new Uri(new Uri(provider.BaseAddress!), "responses")
      );

      requestMessage.Content = JsonContent.Create(
         request,
         options: JsonOptions
      );

      var apiKey = ApiKeySourceResolver.Resolve(provider.ApiKeySource);

      if(!string.IsNullOrWhiteSpace(apiKey))
      {
         requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
      }

      return await HttpClient.SendAsync(requestMessage, cancellationToken);
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
      catch (JsonException)
      {
      }
   }

   private static string NormalizeOutput(string value)
   {
      return value
         .Trim()
         .Trim('"', '\'')
         .ReplaceLineEndings(" ");
   }

   private static string CreateFailureMessage(
      System.Net.HttpStatusCode statusCode,
      string rawResponse
   )
   {
      var preview = rawResponse
         .ReplaceLineEndings(" ")
         .Trim();

      if(preview.Length > 240)
      {
         preview = preview[..240] + "...";
      }

      return $"lmstudio failed with {(int)statusCode} {statusCode}: {preview}";
   }
}
