using System.Text;
using System.Text.RegularExpressions;

namespace SESport.AI.WebPages;

internal static class WebPageStructuredTextSupport
{
   private static readonly string[] PresentationPropertyTokens =
   [
      "animation",
      "aria",
      "banner",
      "background",
      "border",
      "boxshadow",
      "class",
      "color",
      "consent",
      "cookie",
      "config",
      "css",
      "display",
      "excludeuri",
      "fileext",
      "font",
      "height",
      "imageext",
      "layout",
      "margin",
      "opacity",
      "option",
      "padd",
      "pattern",
      "placeholder",
      "position",
      "radius",
      "regex",
      "selector",
      "setting",
      "shadow",
      "spacing",
      "style",
      "template",
      "theme",
      "transform",
      "urltemplate",
      "valuename",
      "width",
      "zindex"
   ];

   private static readonly Regex CssConfigurationValueRegex = new(
      @"^[+-]?(?:\d+(?:\.\d+)?|\.\d+)" +
      @"(?:px|em|rem|%|vh|vw|vmin|vmax|pt|pc|in|cm|mm|ex|ch|s|ms)?" +
      @"(?:\s+[+-]?(?:\d+(?:\.\d+)?|\.\d+)" +
      @"(?:px|em|rem|%|vh|vw|vmin|vmax|pt|pc|in|cm|mm|ex|ch|s|ms)?){1,7}$",
      RegexOptions.CultureInvariant | RegexOptions.Compiled
   );

   private static string NormalizeText(string? text)
   {
      return WebPageContentFetchSupport.NormalizeText(text);
   }
   internal static bool ShouldCaptureEmbeddedValue(
      string? propertyName,
      string? value
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return false;
      }

      var normalizedValue = NormalizeText(value);

      if(normalizedValue.Length < 2 || normalizedValue.Length > 160)
      {
         return false;
      }

      if(IsLikelyMachineValue(normalizedValue))
      {
         return false;
      }

      if(IsLikelyPresentationConfiguration(propertyName, normalizedValue))
      {
         return false;
      }

      if(IsLikelyDisplayProperty(propertyName))
      {
         return true;
      }

      return IsLikelyHumanReadable(normalizedValue);
   }

   private static bool IsLikelyDisplayProperty(string? propertyName)
   {
      if(string.IsNullOrWhiteSpace(propertyName))
      {
         return false;
      }

      var normalizedPropertyName = propertyName.Trim().ToLowerInvariant();

      return normalizedPropertyName.EndsWith(
            "name",
            StringComparison.Ordinal
         ) ||
         normalizedPropertyName.EndsWith("title", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("label", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("text", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("description",
            StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("caption", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith(
            "headline",
            StringComparison.Ordinal
         ) ||
         normalizedPropertyName.EndsWith("standfirst",
            StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("summary", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("alt", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("alttext", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("city", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("countryname",
            StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("displayname",
            StringComparison.Ordinal);
   }

   internal static bool IsLikelyMachineValue(string value)
   {
      return value.Contains("://", StringComparison.Ordinal) ||
         value.Contains("/", StringComparison.Ordinal) ||
      value.Contains("rrn:", StringComparison.Ordinal) ||
      value.Contains("urn:", StringComparison.Ordinal) ||
      value.Contains("data-lazy-", StringComparison.Ordinal) ||
      value.Contains("document.", StringComparison.Ordinal) ||
      value.Contains("function(", StringComparison.Ordinal) ||
      value.Contains("replace(", StringComparison.Ordinal) ||
      value.Contains("=>", StringComparison.Ordinal) ||
      value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) ||
         value.All(char.IsDigit) ||
         Regex.IsMatch(
            value,
            @"^[0-9a-fA-F]{12,}$",
            RegexOptions.CultureInvariant
         ) ||
         LooksLikeEncodedJson(value) ||
         value.StartsWith("eyJ", StringComparison.Ordinal) &&
         value.Length >= 20 &&
         value.All(character =>
            char.IsLetterOrDigit(character) ||
            character is '-' or '_' or '.'
         );
   }

   private static bool LooksLikeEncodedJson(string value)
   {
      if(!value.StartsWith("eyJ", StringComparison.Ordinal) ||
         value.Length < 20)
      {
         return false;
      }

      try
      {
         var decodedBytes = Convert.FromBase64String(value);
         var decodedValue = Encoding.UTF8.GetString(decodedBytes).TrimStart();

         return decodedValue.StartsWith("{", StringComparison.Ordinal) ||
            decodedValue.StartsWith("[", StringComparison.Ordinal);
      }
      catch(FormatException)
      {
         return false;
      }
   }

   private static bool IsLikelyPresentationConfiguration(
      string? propertyName,
      string value
   )
   {
      if(CssConfigurationValueRegex.IsMatch(value))
      {
         return true;
      }

      if(string.IsNullOrWhiteSpace(propertyName))
      {
         return false;
      }

      var normalizedPropertyName = propertyName.Trim().ToLowerInvariant();

      return PresentationPropertyTokens.Any(token =>
         normalizedPropertyName.Contains(token, StringComparison.Ordinal)
      );
   }

   private static bool IsLikelyHumanReadable(string value)
   {
      if(!value.Any(char.IsLetter))
      {
         return false;
      }

      if(value.Contains(" ", StringComparison.Ordinal))
      {
         return true;
      }

      return value.Length <= 5 && value.All(char.IsUpper);
   }

   internal static bool IsLikelyReadableStructuredPhrase(string value)
   {
      if(!IsLikelyHumanReadable(value))
      {
         return false;
      }

      var tokens = value.Split(
         ' ',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      );

      if(tokens.Length < 2 || tokens.Length > 4)
      {
         return false;
      }

      if(tokens.Any(IsCommonStructuredLabelToken))
      {
         return false;
      }

      return tokens.All(token =>
         token.All(character =>
            char.IsLetter(character) ||
            character is '-' or '\'' or '’'
         )
      );
   }

   private static bool IsCommonStructuredLabelToken(string token)
   {
      var normalizedToken = token.Trim().ToLowerInvariant();

      return normalizedToken is
         "count" or
         "no." or
         "no" or
         "name" or
         "title" or
         "label" or
         "text" or
         "description" or
         "summary" or
         "status" or
         "type" or
         "category" or
         "class" or
         "group" or
         "rank" or
         "round" or
         "date" or
         "time" or
         "priority" or
         "eligible" or
         "entry" or
         "entries" or
         "item" or
         "items" or
         "value" or
         "values" or
         "country" or
         "city" or
         "table" or
         "row" or
         "column" or
         "cell" or
         "id" or
         "code" or
         "page" or
         "section" or
         "link" or
         "url";
   }
}
