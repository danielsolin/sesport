using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SESport.Core.AIActivityTeasers;

public sealed class OpenAiResponsesActivityTeaserGenerator
   : IActivityTeaserGenerator
{
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   private readonly HttpClient httpClient;
   private readonly ActivityTeaserGeneratorOptions options;

   public OpenAiResponsesActivityTeaserGenerator(
      HttpClient httpClient,
      ActivityTeaserGeneratorOptions options
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

   public async Task<string> GenerateAsync(
      ActivityTeaserRequest request,
      CancellationToken cancellationToken
   )
   {
      if(IsOpenRouterBaseAddress() && string.IsNullOrWhiteSpace(options.ApiKey))
      {
         throw new InvalidOperationException(
            "OpenRouter API key is not configured. Set AI:Teaser:ApiKey " +
            "or OPENROUTER_API_KEY."
         );
      }

      var prompt = ActivityTeaserPrompt.Create(request);
      var response = await httpClient.PostAsJsonAsync(
         new Uri(options.BaseAddress, "responses"),
         new
         {
            model = options.Model,
            input = prompt
         },
         JsonOptions,
         cancellationToken
      );
      var rawResponse = await response.Content.ReadAsStringAsync(
         cancellationToken
      );

      if(!response.IsSuccessStatusCode)
      {
         throw new HttpRequestException(
            $"teaser generation failed with " +
            $"{(int)response.StatusCode} {response.StatusCode}",
            null,
            response.StatusCode
         );
      }

      return NormalizeTeaser(ExtractOutputText(rawResponse));
   }

   private static string NormalizeTeaser(string value)
   {
      return value
         .Trim()
         .Trim('"', '\'')
         .ReplaceLineEndings(" ");
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

      foreach(var item in output.EnumerateArray())
      {
         if(!item.TryGetProperty("content", out var content))
         {
            continue;
         }

         foreach(var contentItem in content.EnumerateArray())
         {
            if(
               contentItem.TryGetProperty("text", out var text) &&
               text.ValueKind == JsonValueKind.String
            )
            {
               return text.GetString() ?? "";
            }
         }
      }

      return rawResponse;
   }

   private bool IsOpenRouterBaseAddress()
   {
      return string.Equals(
         options.BaseAddress.Host,
         "openrouter.ai",
         StringComparison.OrdinalIgnoreCase
      );
   }
}
