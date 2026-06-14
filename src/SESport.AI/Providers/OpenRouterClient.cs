using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.AI.Validation;

namespace SESport.AI.Providers;

public sealed class OpenRouterClient : IAiProviderClient
{
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   public string Kind => "openrouter";

   public OpenRouterClient(HttpClient httpClient)
   {
      HttpClient = httpClient;
   }

   private HttpClient HttpClient { get; }

   public JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   )
   {
      var payload = new JsonObject
      {
         ["model"] = provider.Model,
         ["messages"] = CreateMessages(renderedPrompt),
         ["plugins"] = new JsonArray
         {
            new JsonObject
            {
               ["id"] = "web"
            }
         }
      };

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
      return payload;
   }

   public async Task<AiJobResult> GenerateAsync(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt,
      string inputPayloadJson,
      CancellationToken cancellationToken,
      Func<string?, CancellationToken, Task>? toolTraceUpdated = null
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
      var requestJson = AiRequestJsonSerializer.Serialize(request);
      var response = await SendAsync(
         provider,
         request,
         cancellationToken
      );
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

      var outputText = ResponsesOutputValidator.ValidateStructuredOutput(
         NormalizeOutput(ExtractFinalText(rawResponse)),
         job.OutputMode,
         prompt.OutputSchemaJson
      );

      return new AiJobResult(
         Guid.NewGuid(),
         job.Id,
         provider.Id,
         provider.Model,
         renderedPrompt.ToPromptText(),
         requestJson,
         outputText,
         rawResponse,
         null,
         0,
         requestJson.Length,
         null,
         null,
         null,
         null
      );
   }

   private static JsonArray CreateMessages(
      AiRenderedPrompt renderedPrompt
   )
   {
      var messages = new JsonArray();

      if(!string.IsNullOrWhiteSpace(renderedPrompt.SystemPrompt))
      {
         messages.Add(
            new JsonObject
            {
               ["role"] = "system",
               ["content"] = renderedPrompt.SystemPrompt.Trim()
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

   private async Task<HttpResponseMessage> SendAsync(
      AiProviderDefinition provider,
      JsonObject request,
      CancellationToken cancellationToken
   )
   {
      var requestMessage = new HttpRequestMessage(
         HttpMethod.Post,
         new Uri(new Uri(provider.BaseAddress!), "chat/completions")
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

      if(string.Equals(provider.Kind, "openrouter", StringComparison.Ordinal))
      {
         requestMessage.Headers.TryAddWithoutValidation(
            "X-OpenRouter-Experimental-Metadata",
            "enabled"
         );
      }

      return await HttpClient.SendAsync(requestMessage, cancellationToken);
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

      if(preview.Length > 1000)
      {
         preview = preview[..1000] + "...";
      }

      return
         $"ai request failed with {(int)statusCode} {statusCode}. " +
         $"Response: {preview}";
   }

   private static string ExtractFinalText(string rawResponse)
   {
      using var document = JsonDocument.Parse(rawResponse);
      var root = document.RootElement;

      if(!root.TryGetProperty("choices", out var choices))
      {
         return rawResponse;
      }

      foreach(var choice in choices.EnumerateArray())
      {
         if(!choice.TryGetProperty("message", out var message))
         {
            continue;
         }

         var content = ExtractMessageContent(message);

         if(!string.IsNullOrWhiteSpace(content))
         {
            return content;
         }
      }

      return rawResponse;
   }

   private static string ExtractMessageContent(JsonElement message)
   {
      if(
         !message.TryGetProperty("content", out var content) ||
         content.ValueKind == JsonValueKind.Null
      )
      {
         return "";
      }

      if(content.ValueKind == JsonValueKind.String)
      {
         return content.GetString() ?? "";
      }

      if(content.ValueKind != JsonValueKind.Array)
      {
         return "";
      }

      var builder = new System.Text.StringBuilder();

      foreach(var contentItem in content.EnumerateArray())
      {
         if(
            contentItem.TryGetProperty("text", out var text) &&
            text.ValueKind == JsonValueKind.String
         )
         {
            builder.Append(text.GetString());
         }
      }

      return builder.ToString();
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
}
