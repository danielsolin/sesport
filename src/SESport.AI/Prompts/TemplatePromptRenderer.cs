using System.Text.Json;
using System.Text.RegularExpressions;

using SESport.AI.Interfaces;
using SESport.Core.AI;

namespace SESport.AI.Prompts;

public sealed class TemplatePromptRenderer : IAiPromptRenderer
{
   private static readonly Regex TokenRegex = new(
      @"\{\{\s*(?<path>[a-zA-Z0-9_.-]+)\s*\}\}",
      RegexOptions.Compiled | RegexOptions.CultureInvariant
   );

   public AiRenderedPrompt Render(
      AiPromptDefinition prompt,
      string inputPayloadJson
   )
   {
      using var document = JsonDocument.Parse(inputPayloadJson);
      var systemPrompt = string.IsNullOrWhiteSpace(prompt.SystemPrompt)
         ? null
         : ReplaceTokens(
            prompt.SystemPrompt,
            document.RootElement
         ).Trim();
      var userPrompt = ReplaceTokens(
         prompt.UserPromptTemplate,
         document.RootElement
      );

      return new AiRenderedPrompt(
         systemPrompt,
         userPrompt.Trim()
      );
   }

   private static string ReplaceTokens(
      string template,
      JsonElement root
   )
   {
      return TokenRegex.Replace(template, match =>
      {
         var path = match.Groups["path"].Value;
         return TryResolvePrimaryCountryPath(path, out var value)
            ? value
            : ResolvePath(root, path);
      });
   }

   private static bool TryResolvePrimaryCountryPath(
      string path,
      out string value
   )
   {
      value = path switch
      {
         "PrimaryCountry.TwoLetterCode" or "TwoLetterCode" =>
            PrimaryCountry.TwoLetterCode,
         "PrimaryCountry.ThreeLetterCode" or "ThreeLetterCode" =>
            PrimaryCountry.ThreeLetterCode,
         "PrimaryCountry.CountryName" or "CountryName" =>
            PrimaryCountry.CountryName,
         "PrimaryCountry.LocalDisplayName" or "LocalDisplayName" =>
            PrimaryCountry.LocalDisplayName,
         "PrimaryCountry.LanguageName" or "LanguageName" =>
            PrimaryCountry.LanguageName,
         _ => string.Empty
      };

      return value.Length > 0;
   }

   private static string ResolvePath(JsonElement root, string path)
   {
      var current = root;

      foreach(
         var segment in path.Split(
            '.',
            StringSplitOptions.RemoveEmptyEntries
         )
      )
      {
         if(current.ValueKind != JsonValueKind.Object ||
            !current.TryGetProperty(segment, out var next))
         {
            return string.Empty;
         }

         current = next;
      }

      return FormatValue(current);
   }

   private static string FormatValue(JsonElement element)
   {
      return element.ValueKind switch
      {
         JsonValueKind.String => element.GetString() ?? string.Empty,
         JsonValueKind.Number => element.GetRawText(),
         JsonValueKind.True => "true",
         JsonValueKind.False => "false",
         JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
         JsonValueKind.Array => FormatArray(element),
         _ => element.GetRawText()
      };
   }

   private static string FormatArray(JsonElement array)
   {
      var values = new List<string>();

      foreach(var item in array.EnumerateArray())
      {
         var value = FormatValue(item);

         if(!string.IsNullOrWhiteSpace(value))
         {
            values.Add(value);
         }
      }

      return string.Join(", ", values);
   }
}
