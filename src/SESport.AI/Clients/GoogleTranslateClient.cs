using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Playwright;

using SESport.Core.AI;

namespace SESport.AI.Clients;

public sealed class GoogleTranslateClient : IAiProviderClient
{
   private readonly Func<string, CancellationToken, Task<string>>
      translationFetcher;

   public GoogleTranslateClient()
      : this(FetchTranslationWithPlaywrightAsync)
   {
   }

   internal GoogleTranslateClient(
      Func<string, CancellationToken, Task<string>> translationFetcher
   )
   {
      this.translationFetcher = translationFetcher;
   }

   public IReadOnlyCollection<string> Kinds =>
      [AiProviderKinds.GoogleTranslate];

   public JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   )
   {
      return new JsonObject
      {
         ["format"] = "text"
      };
   }

   public async Task<AiJobResult> GenerateAsync(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt,
      string inputPayloadJson,
      CancellationToken cancellationToken,
      Func<string?, int, CancellationToken, Task>? toolTraceUpdated = null
   )
   {
      var translation = ParseInput(inputPayloadJson);
      var url = BuildTranslationUrl(provider.BaseAddress, translation);
      var requestJson = JsonSerializer.Serialize(
         new
         {
            from_language = translation.FromLanguage,
            to_language = translation.ToLanguage,
            text = translation.Text
         }
      );

      var outputText = await translationFetcher(
         url,
         cancellationToken
      );

      return new AiJobResult(
         Guid.NewGuid(),
         job.Id,
         provider.Id,
         provider.Model,
         renderedPrompt.ToPromptText(),
         requestJson,
         outputText,
         null,
         null,
         0,
         requestJson.Length,
         null,
         null,
         null,
         null
      );
   }

   private static string BuildTranslationUrl(
      string? baseAddress,
      (string FromLanguage, string ToLanguage, string Text) translation
   )
   {
      if(string.IsNullOrWhiteSpace(baseAddress) ||
         !Uri.TryCreate(
            baseAddress,
            UriKind.Absolute,
            out var parsedBaseAddress
         ) ||
         (parsedBaseAddress.Scheme != Uri.UriSchemeHttp &&
          parsedBaseAddress.Scheme != Uri.UriSchemeHttps))
      {
         throw new InvalidOperationException(
            "Google Translate provider requires an HTTP base address."
         );
      }

      var source = ResolveLanguageCode(translation.FromLanguage);
      var target = ResolveLanguageCode(translation.ToLanguage);
      var url = parsedBaseAddress.ToString().TrimEnd('/') + "/?";

      return url + "sl=" + Uri.EscapeDataString(source)
         + "&tl=" + Uri.EscapeDataString(target)
         + "&text=" + Uri.EscapeDataString(translation.Text)
         + "&op=translate";
   }

   private static (
      string FromLanguage,
      string ToLanguage,
      string Text
   ) ParseInput(string inputPayloadJson)
   {
      using var document = JsonDocument.Parse(inputPayloadJson);
      var root = document.RootElement;

      return (
         GetRequiredString(root, "from_language"),
         GetRequiredString(root, "to_language"),
         GetRequiredString(root, "text")
      );
   }

   private static string GetRequiredString(
      JsonElement root,
      string propertyName
   )
   {
      if(!root.TryGetProperty(propertyName, out var value) ||
         value.ValueKind != JsonValueKind.String ||
         string.IsNullOrWhiteSpace(value.GetString()))
      {
         throw new InvalidOperationException(
            $"Google Translate input is missing '{propertyName}'."
         );
      }

      return value.GetString()!.Trim();
   }

   private static string ResolveLanguageCode(string language)
   {
      var normalized = language.Trim().ToLowerInvariant();

      if(normalized.Length is 2 or 3)
      {
         return normalized;
      }

      return normalized switch
      {
         "english" => "en",
         "swedish" => "sv",
         "german" => "de",
         "french" => "fr",
         "spanish" => "es",
         "italian" => "it",
         "portuguese" => "pt",
         "dutch" => "nl",
         "danish" => "da",
         "norwegian" => "no",
         "finnish" => "fi",
         "polish" => "pl",
         "czech" => "cs",
         "japanese" => "ja",
         "chinese" => "zh",
         _ => throw new InvalidOperationException(
            $"Unsupported language '{language}'. Use a Google language code."
         )
      };
   }

   private static async Task<string> FetchTranslationWithPlaywrightAsync(
      string url,
      CancellationToken cancellationToken
   )
   {
      using var playwright = await Playwright.CreateAsync()
         .WaitAsync(cancellationToken);
      await using var browser = await playwright.Chromium.LaunchAsync(
         new BrowserTypeLaunchOptions
         {
            Headless = true
         }
      ).WaitAsync(cancellationToken);
      await using var context = await browser.NewContextAsync()
         .WaitAsync(cancellationToken);
      await using var page = await context.NewPageAsync()
         .WaitAsync(cancellationToken);

      await page.GotoAsync(
         url,
         new PageGotoOptions
         {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = (float)
               AiDefaults.GoogleTranslatePageTimeout.TotalMilliseconds
         }
      ).WaitAsync(cancellationToken);

      var rejectCookiesButton = page.GetByRole(
         AriaRole.Button,
         new PageGetByRoleOptions
         {
            Name = "Reject all",
            Exact = true
         }
      );
      if(await rejectCookiesButton.CountAsync()
         .WaitAsync(cancellationToken) > 0)
      {
         await rejectCookiesButton.ClickAsync()
            .WaitAsync(cancellationToken);
         await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded)
            .WaitAsync(cancellationToken);
      }

      var result = page.Locator("span[jsname='W297wb']").First;
      await result.WaitForAsync(
         new LocatorWaitForOptions
         {
            State = WaitForSelectorState.Visible,
            Timeout = (float)
               AiDefaults.GoogleTranslatePageTimeout.TotalMilliseconds
         }
      ).WaitAsync(cancellationToken);
      await page.WaitForTimeoutAsync(
         (float)AiDefaults.GoogleTranslateStabilityDelay
            .TotalMilliseconds
      )
         .WaitAsync(cancellationToken);

      var translatedText = string.Join(
         " ",
         (await page
            .Locator("span[jsname='W297wb']")
            .AllInnerTextsAsync()
            .WaitAsync(cancellationToken))
            .Select(text => text.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
      );

      if(string.IsNullOrWhiteSpace(translatedText))
      {
         throw new InvalidOperationException(
            "Google Translate returned an empty translation."
         );
      }

      return WebUtility.HtmlDecode(translatedText);
   }

}
