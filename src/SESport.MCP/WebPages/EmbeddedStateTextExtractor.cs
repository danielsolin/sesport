using System.Net;
using System.Text.RegularExpressions;

namespace SESport.AI.WebPages;

internal static class EmbeddedStateTextExtractor
{
   private static string NormalizeText(string? text)
   {
      return WebPageContentFetchSupport.NormalizeText(text);
   }

   internal static string ExtractText(string html)
   {
      var texts = new List<string>();
      var seenTexts = new HashSet<string>(StringComparer.Ordinal);
      var scriptMatches = Regex.Matches(
         html,
         @"<script\b([^>]*)>(.*?)</script>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );

      foreach(Match match in scriptMatches)
      {
         if(!TryExtractStructuredJsonText(
            match.Groups[1].Value,
            match.Groups[2].Value,
            out var embeddedText
         ))
         {
            continue;
         }

         foreach(var line in embeddedText.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries
         ))
         {
            var normalizedLine = NormalizeText(line);

            if(string.IsNullOrWhiteSpace(normalizedLine) ||
               !seenTexts.Add(normalizedLine))
            {
               continue;
            }

            texts.Add(normalizedLine);
         }
      }

      return NormalizeText(string.Join(Environment.NewLine, texts));
   }

   private static bool TryExtractStructuredJsonText(
      string scriptAttributes,
      string scriptContent,
      out string text
   )
   {
      text = string.Empty;
      var normalizedContent = WebUtility.HtmlDecode(scriptContent).Trim();

      if(string.IsNullOrWhiteSpace(normalizedContent))
      {
         return false;
      }

      if(!scriptAttributes.Contains("application/json",
            StringComparison.OrdinalIgnoreCase) &&
         !scriptAttributes.Contains("application/ld+json",
            StringComparison.OrdinalIgnoreCase) &&
         !LooksLikeStructuredScript(normalizedContent))
      {
         return false;
      }

      if(!TryParseJsonDocument(normalizedContent, out var document))
      {
         return false;
      }

      if(document is null)
      {
         return false;
      }

      using(document)
      {
         var values = new List<string>();
         var seenValues = new HashSet<string>(StringComparer.Ordinal);
         CollectEmbeddedText(
            document.RootElement,
            null,
            values,
            seenValues
         );

         text = NormalizeText(string.Join(Environment.NewLine, values));
         return !string.IsNullOrWhiteSpace(text);
      }
   }

   private static bool LooksLikeStructuredScript(string scriptContent)
   {
      if(scriptContent.StartsWith("{", StringComparison.Ordinal) ||
         scriptContent.StartsWith("[", StringComparison.Ordinal) ||
         scriptContent.Contains("__INITIAL_STATE__",
            StringComparison.Ordinal) ||
         scriptContent.Contains("__NEXT_DATA__", StringComparison.Ordinal) ||
         scriptContent.Contains("prerender-data-cache",
            StringComparison.Ordinal))
      {
         return true;
      }

      return scriptContent.Contains("=", StringComparison.Ordinal) &&
         (scriptContent.Contains("{", StringComparison.Ordinal) ||
          scriptContent.Contains("[", StringComparison.Ordinal));
   }

   private static bool TryParseJsonDocument(
      string content,
      out JsonDocument? document
   )
   {
      document = null;

      if(TryParseJsonDocumentCore(content, out document))
      {
         return true;
      }

      if(!TryExtractJsonFragment(content, out var jsonFragment))
      {
         return false;
      }

      return TryParseJsonDocumentCore(jsonFragment, out document);
   }

   private static bool TryParseJsonDocumentCore(
      string content,
      out JsonDocument? document
   )
   {
      document = null;

      try
      {
         document = JsonDocument.Parse(
            content.Trim().TrimEnd(';'),
            new JsonDocumentOptions
            {
               AllowTrailingCommas = true
            }
         );

         if(document.RootElement.ValueKind == JsonValueKind.String)
         {
            var embeddedJson = document.RootElement.GetString();

            if(!string.IsNullOrWhiteSpace(embeddedJson) &&
               TryParseJsonDocumentCore(embeddedJson, out var nestedDocument))
            {
               document.Dispose();
               document = nestedDocument;
            }
         }

         return true;
      }
      catch(JsonException)
      {
         document?.Dispose();
         document = null;
         return false;
      }
   }

   private static bool TryExtractJsonFragment(
      string content,
      out string jsonFragment
   )
   {
      jsonFragment = string.Empty;

      var startIndex = content.IndexOfAny(['{', '[']);

      if(startIndex < 0)
      {
         return false;
      }

      var endIndex = Math.Max(
         content.LastIndexOf('}'),
         content.LastIndexOf(']')
      );

      if(endIndex <= startIndex)
      {
         return false;
      }

      jsonFragment = content[startIndex..(endIndex + 1)];
      return true;
   }

   private static void CollectEmbeddedText(
      JsonElement element,
      string? propertyName,
      ICollection<string> texts,
      ISet<string> seenTexts
   )
   {
      switch(element.ValueKind)
      {
         case JsonValueKind.Object:
            var properties = element.EnumerateObject().ToList();
            var recordValues = properties
               .Where(property => IsEmbeddedScalar(property.Value))
               .Select(property => (
                  property.Name,
                  Value: property.Value.ToString()
               ))
               .Where(item =>
                  WebPageStructuredTextSupport.ShouldCaptureEmbeddedValue(
                     item.Name,
                     item.Value
                  )
               )
               .Select(item => (
                  item.Name,
                  Value: NormalizeText(item.Value)
               ))
               .Where(item => !string.IsNullOrWhiteSpace(item.Value))
               .ToList();

            if(recordValues.Count > 1)
            {
               var recordText = string.Join(
                  " | ",
                  recordValues.Select(item =>
                     $"{item.Name}: {item.Value}"
                  )
               );

               if(seenTexts.Add(recordText))
               {
                  texts.Add(recordText);
               }
            }

            foreach(var property in properties)
            {
               if(recordValues.Count > 1 &&
                  IsEmbeddedScalar(property.Value))
               {
                  continue;
               }

               CollectEmbeddedText(
                  property.Value,
                  property.Name,
                  texts,
                  seenTexts
               );
            }

            break;
         case JsonValueKind.Array:
            foreach(var item in element.EnumerateArray())
            {
               CollectEmbeddedText(item, propertyName, texts, seenTexts);
            }

            break;
         case JsonValueKind.String:
            AddEmbeddedValue(
               propertyName,
               element.GetString(),
               texts,
               seenTexts
            );

            break;
         case JsonValueKind.Number:
         case JsonValueKind.True:
         case JsonValueKind.False:
            AddEmbeddedValue(
               propertyName,
               element.ToString(),
               texts,
               seenTexts
            );

            break;
      }
   }

   private static bool IsEmbeddedScalar(JsonElement element)
   {
      return element.ValueKind is
         JsonValueKind.String or
         JsonValueKind.Number or
         JsonValueKind.True or
         JsonValueKind.False;
   }

   private static void AddEmbeddedValue(
      string? propertyName,
      string? value,
      ICollection<string> texts,
      ISet<string> seenTexts
   )
   {
      if(!WebPageStructuredTextSupport.ShouldCaptureEmbeddedValue(
            propertyName,
            value
         ))
      {
         return;
      }

      var normalizedValue = NormalizeText(value);

      if(string.IsNullOrWhiteSpace(normalizedValue) ||
         !seenTexts.Add(normalizedValue))
      {
         return;
      }

      texts.Add(normalizedValue);
   }
}
