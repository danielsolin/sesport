using System.Text.Json;
using System.Text;

namespace SESport.Core.AI;

public static class AiRunSummaryFormatter
{
   private const int MaxSummaryLength = 96;
   private const int MaxValueLength = 48;

   public static string Format(string? outputText)
   {
      if(string.IsNullOrWhiteSpace(outputText))
      {
         return string.Empty;
      }

      if(TryFormatJsonSummary(outputText, out var jsonSummary))
      {
         return jsonSummary;
      }

      return FormatPlainTextSummary(outputText);
   }

   private static bool TryFormatJsonSummary(
      string outputText,
      out string summary
   )
   {
      summary = string.Empty;

      try
      {
         using var document = JsonDocument.Parse(outputText);
         summary = FormatJsonValue(document.RootElement);
         return !string.IsNullOrWhiteSpace(summary);
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static string FormatJsonValue(JsonElement value)
   {
      return value.ValueKind switch
      {
         JsonValueKind.Object => FormatJsonObject(value),
         JsonValueKind.Array => FormatCountSummary("items",
            value.GetArrayLength()),
         JsonValueKind.String => FormatTextValue(value.GetString()),
         JsonValueKind.Number => value.GetRawText(),
         JsonValueKind.True => "true",
         JsonValueKind.False => "false",
         JsonValueKind.Null => "null",
         _ => string.Empty
      };
   }

   private static string FormatJsonObject(JsonElement value)
   {
      foreach(var property in value.EnumerateObject())
      {
         if(property.Value.ValueKind == JsonValueKind.Array)
         {
            return FormatCountSummary(
               property.Name,
               property.Value.GetArrayLength()
            );
         }
      }

      foreach(var property in value.EnumerateObject())
      {
         var fragment = FormatJsonProperty(property.Name, property.Value);

         if(!string.IsNullOrWhiteSpace(fragment))
         {
            return fragment;
         }
      }

      var fieldCount = value.EnumerateObject().Count();
      if(fieldCount == 0)
      {
         return "JSON object";
      }

      return $"Object with {fieldCount} field" +
         $"{(fieldCount == 1 ? "" : "s")}";
   }

   private static string FormatJsonProperty(string name, JsonElement value)
   {
      var label = string.IsNullOrWhiteSpace(name)
         ? "value"
         : FormatLabel(name);

      return value.ValueKind switch
      {
         JsonValueKind.Array => FormatCountSummary(
            label,
            value.GetArrayLength()
         ),
         JsonValueKind.Object => $"{label}: {FormatObjectValue(value)}",
         JsonValueKind.String => FormatStringProperty(
            label,
            value.GetString()
         ),
         JsonValueKind.Number => $"{label}: {value.GetRawText()}",
         JsonValueKind.True => $"{label}: true",
         JsonValueKind.False => $"{label}: false",
         JsonValueKind.Null => $"{label}: null",
         _ => string.Empty
      };
   }

   private static string FormatObjectValue(JsonElement value)
   {
      var fieldCount = value.EnumerateObject().Count();

      return fieldCount == 0
         ? "object"
         : $"object with {fieldCount} field" +
            $"{(fieldCount == 1 ? "" : "s")}";
   }

   private static string FormatCountSummary(string name, int count)
   {
      var label = FormatLabel(name);

      if(count == 1)
      {
         label = SingularizeLabel(label);
      }

      return TruncateSummary($"{count} {label}");
   }

   private static string FormatStringProperty(
      string label,
      string? value
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return string.Empty;
      }

      return TruncateSummary($"{label}: {FormatTextValue(value)}");
   }

   private static string FormatTextValue(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return string.Empty;
      }

      var summary = value.ReplaceLineEndings(" ").Trim();

      return TruncateText(summary, MaxValueLength);
   }

   private static string FormatLabel(string value)
   {
      var builder = new StringBuilder(value.Length + 4);

      for(var index = 0; index < value.Length; index++)
      {
         var current = value[index];

         if(current == '_' || current == '-')
         {
            if(builder.Length > 0 &&
               builder[builder.Length - 1] != ' ')
            {
               builder.Append(' ');
            }

            continue;
         }

         if(index > 0 &&
            char.IsUpper(current) &&
            char.IsLower(value[index - 1]))
         {
            builder.Append(' ');
         }
         else if(index > 0 &&
                 char.IsUpper(current) &&
                 char.IsUpper(value[index - 1]) &&
                 index + 1 < value.Length &&
                 char.IsLower(value[index + 1]))
         {
            builder.Append(' ');
         }

         builder.Append(char.ToLowerInvariant(current));
      }

      return builder.ToString().Trim();
   }

   private static string SingularizeLabel(string value)
   {
      if(value.EndsWith("ies", StringComparison.Ordinal))
      {
         return value[..^3] + "y";
      }

      if(value.Length > 1 &&
         value.EndsWith("s", StringComparison.Ordinal) &&
         !value.EndsWith("ss", StringComparison.Ordinal))
      {
         return value[..^1];
      }

      return value;
   }

   private static string FormatPlainTextSummary(string outputText)
   {
      return TruncateSummary(outputText.ReplaceLineEndings(" ").Trim());
   }

   private static string TruncateSummary(string value)
   {
      return TruncateText(value, MaxSummaryLength);
   }

   private static string TruncateText(string value, int maxLength)
   {
      if(value.Length <= maxLength)
      {
         return value;
      }

      if(maxLength <= 3)
      {
         return value[..maxLength];
      }

      return value[..(maxLength - 3)] + "...";
   }
}
