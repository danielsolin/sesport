using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SESport.Core.AIActivitySearch;

public sealed class OpenAiResponsesActivitySearchClient
   : IActivitySearchModelClient
{
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

      if (!string.IsNullOrWhiteSpace(options.ApiKey))
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
      var requestPayload = CreateRequestPayload(request);
      var response = await httpClient.PostAsJsonAsync(
         new Uri(options.BaseAddress, "responses"),
         requestPayload,
         JsonOptions,
         cancellationToken
      );

      var rawResponse = await response.Content.ReadAsStringAsync(
         cancellationToken
      );

      if (!response.IsSuccessStatusCode)
      {
         var providerHint = options.BaseAddress.Host.Equals(
            "openrouter.ai",
            StringComparison.OrdinalIgnoreCase
         )
            ? " Check that OPENROUTER_API_KEY is current and belongs to an " +
              "active OpenRouter account."
            : "";

         throw new HttpRequestException(
            $"AI activity search failed with {(int)response.StatusCode} " +
            $"{response.StatusCode} from {options.BaseAddress}: " +
            $"{ExtractErrorMessage(rawResponse)}{providerHint}"
         );
      }

      var rawContent = ExtractOutputText(rawResponse);
      var proposals = ActivitySearchResponseParser.ParseProposals(
         rawContent,
         JsonOptions
      );

      return new ActivitySearchModelResult(
         rawContent,
         rawResponse,
         proposals
      );
   }

   private object CreateRequestPayload(ActivitySearchRequest request)
   {
      var tools = request.AllowWebSearch
         ? new object[] { new { type = options.WebSearchToolType } }
         : [];

      return new
      {
         model = options.Model,
         input = ActivitySearchPrompt.Create(request),
         tools,
         tool_choice = request.AllowWebSearch ? "auto" : null
      };
   }

   private static string ExtractOutputText(string rawResponse)
   {
      using var document = JsonDocument.Parse(rawResponse);
      var root = document.RootElement;

      if (
         root.TryGetProperty("output_text", out var outputText) &&
         outputText.ValueKind == JsonValueKind.String
      )
      {
         return outputText.GetString() ?? "";
      }

      if (!root.TryGetProperty("output", out var output))
      {
         return rawResponse;
      }

      var fallbackText = "";

      foreach (var item in output.EnumerateArray())
      {
         if (!item.TryGetProperty("content", out var content))
         {
            continue;
         }

         foreach (var contentItem in content.EnumerateArray())
         {
            if (
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

            if (
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

   private static string ExtractErrorMessage(string rawResponse)
   {
      try
      {
         using var document = JsonDocument.Parse(rawResponse);
         var root = document.RootElement;

         if (
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

}
