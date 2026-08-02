using System.Text;
using System.Text.Json;

namespace SESport.Core.AI;

public static class AiRunSummaryFormatter
{
   private const int MaxSummaryLength = 96;
   private const int MaxValueLength = 48;

   public static string Format(
      string? outputText,
      string? jobId = null,
      string? outputSchemaJson = null
   )
   {
      if(string.IsNullOrWhiteSpace(outputText))
      {
         return string.Empty;
      }

      if(string.Equals(
         jobId,
         AiJobIds.FindPersonData,
         StringComparison.Ordinal
      ) && TryFormatPersonFactsSummary(outputText, out var factsSummary))
      {
         return factsSummary;
      }

      if(TryFormatJsonSummary(
         outputText,
         outputSchemaJson,
         out var jsonSummary
      ))
      {
         return jsonSummary;
      }

      return FormatPlainTextSummary(outputText);
   }

   private static bool TryFormatPersonFactsSummary(
      string outputText,
      out string summary
   )
   {
      summary = string.Empty;

      try
      {
         using var document = JsonDocument.Parse(outputText);
         if(document.RootElement.ValueKind != JsonValueKind.Object)
         {
            return false;
         }

         var fragments = new List<string>();
         foreach(var name in new[]
         {
            "birthdate",
            "height",
            "weight",
            "formative_club"
         })
         {
            if(!document.RootElement.TryGetProperty(name, out var value) ||
               value.ValueKind == JsonValueKind.Null)
            {
               continue;
            }

            var fragment = FormatJsonProperty(name, value);
            if(!string.IsNullOrWhiteSpace(fragment))
            {
               fragments.Add(fragment);
            }
         }

         summary = TruncateSummary(string.Join(", ", fragments));
         return summary.Length > 0;
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static bool TryFormatJsonSummary(
      string outputText,
      string? outputSchemaJson,
      out string summary
   )
   {
      summary = string.Empty;

      try
      {
         using var document = JsonDocument.Parse(outputText);
         using var schemaDocument = TryParseJsonDocument(
            outputSchemaJson
         );
         JsonElement? schemaRoot = schemaDocument is null
            ? null
            : schemaDocument.RootElement;
         summary = FormatJsonValue(document.RootElement, schemaRoot);
         return !string.IsNullOrWhiteSpace(summary);
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static JsonDocument? TryParseJsonDocument(string? json)
   {
      if(string.IsNullOrWhiteSpace(json))
      {
         return null;
      }

      try
      {
         return JsonDocument.Parse(json);
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static string FormatJsonValue(
      JsonElement value,
      JsonElement? schema
   )
   {
      return value.ValueKind switch
      {
         JsonValueKind.Object => FormatJsonObject(value, schema),
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

   private static string FormatJsonObject(
      JsonElement value,
      JsonElement? schema
   )
   {
      foreach(var property in value.EnumerateObject())
      {
         if(property.Value.ValueKind == JsonValueKind.Array)
         {
            if(TryFormatArrayFieldSummary(
               property.Name,
               property.Value,
               FindArraySchema(schema, property.Name),
               out var fieldSummary
            ))
            {
               return fieldSummary;
            }

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

   private static bool TryFormatArrayFieldSummary(
      string arrayName,
      JsonElement value,
      JsonElement? arraySchema,
      out string summary
   )
   {
      summary = string.Empty;

      if(value.ValueKind != JsonValueKind.Array ||
         value.GetArrayLength() == 0)
      {
         return false;
      }

      var fieldNames = new List<string>();
      var knownFieldNames = new HashSet<string>(
         StringComparer.Ordinal
      );
      var objectCount = 0;

      foreach(var item in value.EnumerateArray())
      {
         if(item.ValueKind != JsonValueKind.Object)
         {
            continue;
         }

         objectCount++;
         foreach(var property in item.EnumerateObject())
         {
            if(knownFieldNames.Add(property.Name))
            {
               fieldNames.Add(property.Name);
            }
         }
      }

      if(objectCount != value.GetArrayLength())
      {
         return false;
      }

      var schemaFieldName = FindNullableArrayItemFieldName(arraySchema);
      if(schemaFieldName is not null)
      {
         var actualFieldName = fieldNames.FirstOrDefault(fieldName =>
            string.Equals(
               fieldName,
               schemaFieldName,
               StringComparison.OrdinalIgnoreCase
            )
         );

         if(actualFieldName is not null)
         {
            return FormatArrayFieldSummary(
               arrayName,
               value,
               actualFieldName,
               out summary
            );
         }
      }

      var candidates = new List<(string Name, int Count, int Order)>();
      for(var index = 0; index < fieldNames.Count; index++)
      {
         var fieldName = fieldNames[index];
         var count = 0;

         foreach(var item in value.EnumerateArray())
         {
            if(item.TryGetProperty(fieldName, out var field) &&
               HasValue(field))
            {
               count++;
            }
         }

         if(count < objectCount)
         {
            candidates.Add((fieldName, count, index));
         }
      }

      if(candidates.Count == 0)
      {
         return false;
      }

      var candidate = candidates
         .OrderBy(item => item.Count)
         .ThenBy(item => item.Order)
         .First();

      return FormatArrayFieldSummary(
         arrayName,
         value,
         candidate.Name,
         out summary
      );
   }

   private static bool FormatArrayFieldSummary(
      string arrayName,
      JsonElement array,
      string fieldName,
      out string summary
   )
   {
      var count = 0;

      foreach(var item in array.EnumerateArray())
      {
         if(item.TryGetProperty(fieldName, out var field) &&
            HasValue(field))
         {
            count++;
         }
      }

      var arraySummary = FormatCountSummary(
         arrayName,
         array.GetArrayLength()
      );
      var fieldSummary = FormatCountSummary(fieldName, count);
      summary = TruncateSummary($"{arraySummary}, {fieldSummary}");
      return true;
   }

   private static JsonElement? FindArraySchema(
      JsonElement? schema,
      string arrayName
   )
   {
      if(schema is not JsonElement root ||
         root.ValueKind != JsonValueKind.Object ||
         !root.TryGetProperty("properties", out var properties) ||
         properties.ValueKind != JsonValueKind.Object)
      {
         return null;
      }

      if(properties.TryGetProperty(arrayName, out var exactProperty))
      {
         return exactProperty;
      }

      foreach(var property in properties.EnumerateObject())
      {
         if(string.Equals(
            property.Name,
            arrayName,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            return property.Value;
         }
      }

      return null;
   }

   private static string? FindNullableArrayItemFieldName(
      JsonElement? arraySchema
   )
   {
      if(arraySchema is not JsonElement array ||
         array.ValueKind != JsonValueKind.Object ||
         !array.TryGetProperty("items", out var items) ||
         items.ValueKind != JsonValueKind.Object ||
         !items.TryGetProperty("properties", out var properties) ||
         properties.ValueKind != JsonValueKind.Object)
      {
         return null;
      }

      foreach(var property in properties.EnumerateObject())
      {
         if(IsNullableSchemaProperty(property.Value))
         {
            return property.Name;
         }
      }

      return null;
   }

   private static bool IsNullableSchemaProperty(JsonElement property)
   {
      if(property.ValueKind != JsonValueKind.Object)
      {
         return false;
      }

      if(property.TryGetProperty("type", out var type))
      {
         if(type.ValueKind == JsonValueKind.Array &&
            type.EnumerateArray().Any(item =>
               item.ValueKind == JsonValueKind.String &&
               string.Equals(
                  item.GetString(),
                  "null",
                  StringComparison.Ordinal
               )))
         {
            return true;
         }

         if(type.ValueKind == JsonValueKind.String &&
            string.Equals(
               type.GetString(),
               "null",
               StringComparison.Ordinal
            ))
         {
            return true;
         }
      }

      return false;
   }

   private static bool HasValue(JsonElement value)
   {
      if(value.ValueKind == JsonValueKind.Null ||
         value.ValueKind == JsonValueKind.Undefined)
      {
         return false;
      }

      return value.ValueKind != JsonValueKind.String ||
         !string.IsNullOrWhiteSpace(value.GetString());
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
      else
      {
         label = PluralizeLabel(label);
      }

      return TruncateSummary($"{count} {label}");
   }

   private static string PluralizeLabel(string value)
   {
      if(value.EndsWith('s'))
      {
         return value;
      }

      if(value.EndsWith('y') && value.Length > 1 &&
         !IsVowel(value[^2]))
      {
         return value[..^1] + "ies";
      }

      return value + "s";
   }

   private static bool IsVowel(char value)
   {
      return value is 'a' or 'e' or 'i' or 'o' or 'u';
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
         value.EndsWith('s') &&
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
