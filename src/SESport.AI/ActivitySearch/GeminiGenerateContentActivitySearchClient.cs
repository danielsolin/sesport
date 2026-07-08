using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SESport.AI.Interfaces;

namespace SESport.AI.ActivitySearch;

public sealed class GeminiGenerateContentActivitySearchClient
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
   private readonly GeminiGenerateContentActivitySearchClientOptions options;

   public GeminiGenerateContentActivitySearchClient(
      HttpClient httpClient,
      GeminiGenerateContentActivitySearchClientOptions options
   )
   {
      this.httpClient = httpClient;
      this.options = options;

      if(!string.IsNullOrWhiteSpace(options.ApiKey))
      {
         httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "x-goog-api-key",
            options.ApiKey
         );
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
            CreateGenerateContentUri(),
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
               $"Gemini activity search failed with " +
               $"{(int)response.StatusCode} {response.StatusCode}: " +
               ExtractErrorMessage(rawResponse),
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
               PrefixProducer("gemini", options.Model),
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

      throw new InvalidOperationException("Gemini search request was not sent.");
   }

   private Uri CreateGenerateContentUri()
   {
      return new Uri(
         options.BaseAddress,
         $"models/{options.Model}:generateContent"
      );
   }

   private static object CreateRequestPayload(string prompt)
   {
      return new
      {
         contents = new[]
         {
            new
            {
               role = "user",
               parts = new[]
               {
                  new { text = prompt }
               }
            }
         },
         tools = new object[]
         {
            new Dictionary<string, object>
            {
               ["google_search"] = new { }
            }
         },
         generationConfig = new
         {
            temperature = 0.1
         }
      };
   }

   private static string ExtractOutputText(string rawResponse)
   {
      using var document = JsonDocument.Parse(rawResponse);
      var root = document.RootElement;

      if(!root.TryGetProperty("candidates", out var candidates))
      {
         return rawResponse;
      }

      var parts = candidates
         .EnumerateArray()
         .SelectMany(candidate =>
            candidate.TryGetProperty("content", out var content) &&
            content.TryGetProperty("parts", out var contentParts)
               ? contentParts.EnumerateArray()
               : []
         )
         .Where(part =>
            part.TryGetProperty("text", out var text) &&
            text.ValueKind == JsonValueKind.String
         )
         .Select(part => part.GetProperty("text").GetString())
         .Where(text => !string.IsNullOrWhiteSpace(text))
         .ToList();

      return parts.Count == 0
         ? rawResponse
         : string.Join("\n", parts);
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
         "Gemini activity search failed with malformed JSON response " +
         $"envelope after {attempt} attempt(s): status " +
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
