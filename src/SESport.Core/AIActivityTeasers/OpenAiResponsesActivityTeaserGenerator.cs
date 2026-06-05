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

   public async Task<ActivityTeaserGenerationResult> GenerateAsync(
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
            input = prompt,
            response_format = new
            {
               type = "json_object"
            },
            reasoning = new
            {
               effort = "none",
               exclude = true
            }
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

      return new ActivityTeaserGenerationResult(
         prompt,
         NormalizeTeaser(ExtractOutputText(rawResponse))
      );
   }

   private static string NormalizeTeaser(string value)
   {
      var teaser = ExtractTeaser(value) ?? value;

      return teaser
         .Trim()
         .Trim('"', '\'')
         .ReplaceLineEndings(" ");
   }

   private static string? ExtractTeaser(string value)
   {
      foreach(var candidate in GetTeaserCandidates(value))
      {
         var teaser = ExtractTeaserFromJson(candidate);

         if(teaser is not null)
         {
            return teaser;
         }
      }

      return null;
   }

   private static IEnumerable<string> GetTeaserCandidates(string value)
   {
      yield return value;

      var codeFenceContent = ExtractCodeFenceContent(value);

      if(!string.IsNullOrWhiteSpace(codeFenceContent) &&
         !string.Equals(codeFenceContent, value, StringComparison.Ordinal))
      {
         yield return codeFenceContent;
      }
   }

   private static string? ExtractTeaserFromJson(string value)
   {
      try
      {
         using var document = JsonDocument.Parse(value);
         var root = document.RootElement;

         if(root.ValueKind != JsonValueKind.Object)
         {
            return null;
         }

         if(root.TryGetProperty("teaser", out var teaser) &&
            teaser.ValueKind == JsonValueKind.String)
         {
            return teaser.GetString();
         }
      }
      catch (JsonException)
      {
      }

      return null;
   }

   private static string? ExtractCodeFenceContent(string value)
   {
      var trimmed = value.Trim();

      if(!trimmed.StartsWith("```", StringComparison.Ordinal))
      {
         return null;
      }

      var openingFenceEnd = trimmed.IndexOf('\n');

      if(openingFenceEnd < 0)
      {
         return null;
      }

      var closingFenceStart = trimmed.LastIndexOf(
         "```",
         StringComparison.Ordinal
      );

      if(closingFenceStart <= openingFenceEnd)
      {
         return null;
      }

      var content = trimmed.Substring(
         openingFenceEnd + 1,
         closingFenceStart - openingFenceEnd - 1
      ).Trim();

      if(content.StartsWith("json", StringComparison.OrdinalIgnoreCase))
      {
         content = content[4..].TrimStart();
      }

      return content;
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
