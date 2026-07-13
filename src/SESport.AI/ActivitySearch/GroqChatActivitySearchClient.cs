using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using SESport.AI.Interfaces;

namespace SESport.AI.ActivitySearch;

public sealed class GroqChatActivitySearchClient : IActivitySearchModelClient
{
   private const int MaxMalformedResponseAttempts = 3;

   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   private readonly HttpClient httpClient;
   private readonly GroqChatActivitySearchClientOptions options;

   public GroqChatActivitySearchClient(
      HttpClient httpClient,
      GroqChatActivitySearchClientOptions options
   )
   {
      this.httpClient = httpClient;
      this.options = options;

      if(!string.IsNullOrWhiteSpace(options.ApiKey))
      {
         httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ApiKey);
      }
   }

   public async Task<ActivitySearchModelResult> SearchAsync(
      ActivitySearchRequest request,
      CancellationToken cancellationToken
   )
   {
      var prompt = ActivitySearchPrompt.Create(request);
      var requestPayload = CreateRequestPayload(prompt);

      for(var attempt = 1; attempt <= MaxMalformedResponseAttempts; attempt++)
      {
         var response = await httpClient.PostAsJsonAsync(
            new Uri(options.BaseAddress, "chat/completions"),
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
               $"Groq activity search failed with " +
               $"{(int)response.StatusCode} {response.StatusCode}: " +
               ExtractErrorMessage(response, rawResponse),
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
               PrefixProducer("groq", ExtractProducer(rawResponse)),
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

            await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
         }
      }

      throw new InvalidOperationException("Groq search request was not sent.");
   }

   private object CreateRequestPayload(string prompt)
   {
      return new
      {
         model = options.Model,
         messages = new[]
         {
            new
            {
               role = "user",
               content = prompt
            }
         },
         temperature = 0.1,
         compound_custom = new
         {
            tools = new
            {
               enabled_tools = new[] { WebToolNames.Search }
            }
         },
         search_settings = new
         {
            country = "sweden"
         }
      };
   }

   private static string ExtractOutputText(string rawResponse)
   {
      using var document = JsonDocument.Parse(rawResponse);
      var root = document.RootElement;

      if(!root.TryGetProperty("choices", out var choices))
      {
         return rawResponse;
      }

      var parts = choices
         .EnumerateArray()
         .Where(choice => choice.TryGetProperty("message", out _))
         .Select(choice => choice.GetProperty("message"))
         .Where(message =>
            message.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.String
         )
         .Select(message => message.GetProperty("content").GetString())
         .Where(text => !string.IsNullOrWhiteSpace(text))
         .ToList();

      return parts.Count == 0 ? rawResponse : string.Join("\n", parts);
   }

   private string ExtractProducer(string rawResponse)
   {
      try
      {
         using var document = JsonDocument.Parse(rawResponse);
         var root = document.RootElement;

         if(
            root.TryGetProperty("model", out var model) &&
            model.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(model.GetString())
         )
         {
            return model.GetString()!;
         }
      }
      catch(JsonException)
      {
         return options.Model;
      }

      return options.Model;
   }

   private static string ExtractErrorMessage(
      HttpResponseMessage response,
      string rawResponse
   )
   {
      var message = rawResponse;

      try
      {
         using var document = JsonDocument.Parse(rawResponse);
         var root = document.RootElement;

         if(
            root.TryGetProperty("error", out var error) &&
            error.TryGetProperty("message", out var errorMessage) &&
            errorMessage.ValueKind == JsonValueKind.String
         )
         {
            message = errorMessage.GetString() ?? rawResponse;
         }
      }
      catch(JsonException)
      {
      }

      if(response.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
      {
         return string.IsNullOrWhiteSpace(message)
            ? "Request Entity Too Large. Try groq/compound-mini or lower --max."
            : $"{message}. Try groq/compound-mini or lower --max.";
      }

      return message;
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
         "Groq activity search failed with malformed JSON response envelope " +
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

   private static string PrefixProducer(string prefix, string model)
   {
      return model.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase)
         ? model
         : $"{prefix}/{model}";
   }
}
