using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SESport.AI.Interfaces;

namespace SESport.AI.ActivitySearch;

public sealed class OpenAiResponsesActivitySearchClient
   : IActivitySearchModelClient
{
   private const int MaxMalformedResponseAttempts = 3;

   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   private readonly HttpClient httpClient;
   private readonly OpenAiResponsesActivitySearchClientOptions options;

   public OpenAiResponsesActivitySearchClient(
      HttpClient httpClient,
      OpenAiResponsesActivitySearchClientOptions options
   )
   {
      this.httpClient = httpClient;
      this.options = options;

      if(!string.IsNullOrWhiteSpace(options.ApiKey))
      {
         httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ApiKey);
      }

      if(IsOpenRouterBaseAddress())
      {
         httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-OpenRouter-Experimental-Metadata",
            "enabled"
         );
      }
   }

   public async Task<ActivitySearchModelResult> SearchAsync(
      ActivitySearchRequest request,
      CancellationToken cancellationToken
   )
   {
      var prompt = ActivitySearchPrompt.Create(request);
      var requestPayload = CreateRequestPayload(request, prompt);

      for(var attempt = 1; attempt <= MaxMalformedResponseAttempts; attempt++)
      {
         var response = await httpClient.PostAsJsonAsync(
            new Uri(options.BaseAddress, "responses"),
            requestPayload,
            JsonOptions,
            cancellationToken
         );

         var rawResponse = await response.Content.ReadAsStringAsync(
            cancellationToken
         );

         if(!response.IsSuccessStatusCode)
         {
            throw new HttpRequestException(
               $"search failed with " +
               $"{(int)response.StatusCode} {response.StatusCode}",
               null,
               response.StatusCode
            );
         }

         try
         {
            var rawContent = ExtractOutputText(rawResponse);
            var proposals = ActivitySearchResponseParser.ParseProposals(
               rawContent,
               JsonOptions
            );

            return new ActivitySearchModelResult(
               rawContent,
               rawResponse,
               proposals,
               ExtractProducer(rawResponse),
               prompt
            );
         }
         catch(JsonException exception)
         {
            if(attempt == MaxMalformedResponseAttempts)
            {
               throw CreateMalformedResponseException(
                  response,
                  rawResponse,
                  attempt,
                  exception
               );
            }

            await Task.Delay(
               TimeSpan.FromSeconds(attempt),
               cancellationToken
            );
         }
      }

      throw new InvalidOperationException("AI search request was not sent.");
   }

   private object CreateRequestPayload(
      ActivitySearchRequest request,
      string prompt
   )
   {
      var obj = new
      {
         model = options.Model,
         input = prompt,
         tools = new object[] { new { type = options.WebSearchToolType } },
         tool_choice = "auto"
      };

      return obj;
   }

   private static string ExtractOutputText(string rawResponse)
   {
      using var document = JsonDocument.Parse(rawResponse);
      var root = document.RootElement;

      if(
         root.TryGetProperty("output_text", out var outputText) &&
         outputText.ValueKind == JsonValueKind.String
      )
      {
         return outputText.GetString() ?? "";
      }

      if(!root.TryGetProperty("output", out var output))
      {
         return rawResponse;
      }

      var fallbackText = "";

      foreach(var item in output.EnumerateArray())
      {
         if(!item.TryGetProperty("content", out var content))
         {
            continue;
         }

         foreach(var contentItem in content.EnumerateArray())
         {
            if(
               item.TryGetProperty("type", out var itemType) &&
               itemType.GetString() == "message" &&
               contentItem.TryGetProperty("type", out var contentType) &&
               contentType.GetString() == "output_text" &&
               contentItem.TryGetProperty("text", out var text) &&
               text.ValueKind == JsonValueKind.String
            )
            {
               return text.GetString() ?? "";
            }

            if(
               string.IsNullOrWhiteSpace(fallbackText) &&
               contentItem.TryGetProperty("text", out var fallback) &&
               fallback.ValueKind == JsonValueKind.String
            )
            {
               fallbackText = fallback.GetString() ?? "";
            }
         }
      }

      return string.IsNullOrWhiteSpace(fallbackText)
         ? rawResponse
         : fallbackText;
   }

   private string? ExtractProducer(string rawResponse)
   {
      var model = IsOpenRouterBaseAddress()
         ? ExtractOpenRouterModel(rawResponse)
         : ExtractRootModel(rawResponse);

      if(string.IsNullOrWhiteSpace(model))
      {
         model = options.Model;
      }

      return IsOpenRouterBaseAddress()
         ? PrefixProducer("openrouter", model)
         : model;
   }

   private static string? ExtractOpenRouterModel(string rawResponse)
   {
      try
      {
         using var document = JsonDocument.Parse(rawResponse);
         var root = document.RootElement;

         if(
            root.TryGetProperty("openrouter_metadata", out var metadata) &&
            TryGetSelectedEndpointModel(metadata, out var endpointModel)
         )
         {
            return endpointModel;
         }

         if(
            root.TryGetProperty("openrouter_metadata", out metadata) &&
            TryGetSuccessfulAttemptModel(metadata, out var attemptModel)
         )
         {
            return attemptModel;
         }

         return TryGetStringProperty(root, "model", out var rootModel)
            ? rootModel
            : null;
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static string? ExtractRootModel(string rawResponse)
   {
      try
      {
         using var document = JsonDocument.Parse(rawResponse);
         var root = document.RootElement;

         return TryGetStringProperty(root, "model", out var model)
            ? model
            : null;
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static bool TryGetSelectedEndpointModel(
      JsonElement metadata,
      out string? model
   )
   {
      model = null;

      if(
         metadata.ValueKind != JsonValueKind.Object ||
         !metadata.TryGetProperty("endpoints", out var endpoints) ||
         !endpoints.TryGetProperty("available", out var available) ||
         available.ValueKind != JsonValueKind.Array
      )
      {
         return false;
      }

      foreach(var endpoint in available.EnumerateArray())
      {
         if(
            endpoint.TryGetProperty("selected", out var selected) &&
            selected.ValueKind == JsonValueKind.True &&
            TryGetStringProperty(endpoint, "model", out model)
         )
         {
            return true;
         }
      }

      return false;
   }

   private static bool TryGetSuccessfulAttemptModel(
      JsonElement metadata,
      out string? model
   )
   {
      model = null;

      if(
         metadata.ValueKind != JsonValueKind.Object ||
         !metadata.TryGetProperty("attempts", out var attempts) ||
         attempts.ValueKind != JsonValueKind.Array
      )
      {
         return false;
      }

      foreach(var attempt in attempts.EnumerateArray())
      {
         if(
            attempt.TryGetProperty("status", out var status) &&
            status.ValueKind == JsonValueKind.Number &&
            status.GetInt32() >= 200 &&
            status.GetInt32() < 300 &&
            TryGetStringProperty(attempt, "model", out model)
         )
         {
            return true;
         }
      }

      return false;
   }

   private static bool TryGetStringProperty(
      JsonElement element,
      string propertyName,
      out string? value
   )
   {
      value = null;

      if(
         element.ValueKind == JsonValueKind.Object &&
         element.TryGetProperty(propertyName, out var property) &&
         property.ValueKind == JsonValueKind.String
      )
      {
         value = property.GetString();

         return !string.IsNullOrWhiteSpace(value);
      }

      return false;
   }

   private bool IsOpenRouterBaseAddress()
   {
      return options.BaseAddress.Host.Equals(
         "openrouter.ai",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static string PrefixProducer(string prefix, string model)
   {
      return model.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase)
         ? model
         : $"{prefix}/{model}";
   }

   private static string ExtractErrorMessage(string rawResponse)
   {
      try
      {
         using var document = JsonDocument.Parse(rawResponse);
         var root = document.RootElement;

         if(
            root.TryGetProperty("error", out var error) &&
            error.TryGetProperty("message", out var message) &&
            message.ValueKind == JsonValueKind.String
         )
         {
            return message.GetString() ?? rawResponse;
         }
      }
      catch(JsonException)
      {
         return rawResponse;
      }

      return rawResponse;
   }

   private static HttpRequestException CreateMalformedResponseException(
      HttpResponseMessage response,
      string rawResponse,
      int attempt,
      JsonException exception
   )
   {
      var contentType = response.Content.Headers.ContentType?.ToString() ??
         "unknown";
      var snippet = CreateResponseSnippet(rawResponse);

      return new HttpRequestException(
         "search failed with malformed JSON response envelope " +
         $"after {attempt} attempt(s): status " +
         $"{(int)response.StatusCode} {response.StatusCode}, " +
         $"content-type {contentType}, length {rawResponse.Length}, " +
         $"snippet '{snippet}'",
         exception,
         response.StatusCode
      );
   }

   private static string CreateResponseSnippet(string rawResponse)
   {
      var snippet = rawResponse.Trim();

      if(snippet.Length > 200)
      {
         snippet = snippet[..200];
      }

      return snippet
         .Replace("\r", "\\r")
         .Replace("\n", "\\n");
   }

}
