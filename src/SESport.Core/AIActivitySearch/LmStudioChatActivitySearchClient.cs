using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SESport.Core.AIActivitySearch;

public sealed class LmStudioChatActivitySearchClient
   : IActivitySearchModelClient
{
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   private readonly HttpClient httpClient;
   private readonly LmStudioChatActivitySearchClientOptions options;

   public LmStudioChatActivitySearchClient(
      HttpClient httpClient,
      LmStudioChatActivitySearchClientOptions options
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
      var response = await httpClient.PostAsJsonAsync(
         new Uri(options.BaseAddress, "chat"),
         CreateRequestPayload(request),
         JsonOptions,
         cancellationToken
      );

      var rawResponse = await response.Content.ReadAsStringAsync(
         cancellationToken
      );

      if (!response.IsSuccessStatusCode)
      {
         throw new HttpRequestException(
            $"LM Studio activity search failed with " +
            $"{(int)response.StatusCode}: {rawResponse}",
            null,
            response.StatusCode
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
      object[] integrations = request.AllowWebSearch
         ? [
            new
            {
               type = "plugin",
               id = options.PluginId,
               allowed_tools = options.AllowedTools
            }
         ]
         : [];

      return new
      {
         model = options.Model,
         input = ActivitySearchPrompt.Create(request),
         integrations,
         temperature = 0.1,
         store = true
      };
   }

   private static string ExtractOutputText(string rawResponse)
   {
      using var document = JsonDocument.Parse(rawResponse);
      var root = document.RootElement;

      if (!root.TryGetProperty("output", out var output))
      {
         return rawResponse;
      }

      var fallbackText = "";

      foreach (var item in output.EnumerateArray())
      {
         if (
            item.TryGetProperty("type", out var itemType) &&
            itemType.GetString() == "message" &&
            TryGetMessageContent(item, out var messageContent)
         )
         {
            return messageContent;
         }

         if (
            string.IsNullOrWhiteSpace(fallbackText) &&
            TryGetMessageContent(item, out var fallback)
         )
         {
            fallbackText = fallback;
         }
      }

      return string.IsNullOrWhiteSpace(fallbackText)
         ? rawResponse
         : fallbackText;
   }

   private static bool TryGetMessageContent(
      JsonElement item,
      out string content
   )
   {
      content = "";

      if (!item.TryGetProperty("content", out var contentElement))
      {
         return false;
      }

      if (contentElement.ValueKind == JsonValueKind.String)
      {
         content = contentElement.GetString() ?? "";

         return true;
      }

      if (contentElement.ValueKind != JsonValueKind.Array)
      {
         return false;
      }

      foreach (var contentItem in contentElement.EnumerateArray())
      {
         if (
            contentItem.TryGetProperty("text", out var text) &&
            text.ValueKind == JsonValueKind.String
         )
         {
            content = text.GetString() ?? "";

            return true;
         }
      }

      return false;
   }
}
