using System.Text.Json;
using System.Text.RegularExpressions;

namespace SESport.AI.Validation;

public static class ResponsesOutputValidator
{
   public static string ExtractFinalText(string rawResponse)
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

      if(
         !root.TryGetProperty("output", out var output) ||
         output.ValueKind != JsonValueKind.Array
      )
      {
         return rawResponse;
      }

      foreach(var item in output.EnumerateArray())
      {
         if(!IsMessageItem(item))
         {
            continue;
         }

         var text = ExtractMessageText(item);

         if(!string.IsNullOrWhiteSpace(text))
         {
            return text;
         }
      }

      return rawResponse;
   }

   public static string ValidateStructuredOutput(
      string outputText,
      string outputMode,
      string? outputSchemaJson
   )
   {
      outputText = NormalizeStructuredJsonOutput(outputText);

      if(!string.IsNullOrWhiteSpace(outputSchemaJson))
      {
         EnsureSchemaConformity(outputText, outputSchemaJson);
         return outputText;
      }

      if(string.Equals(
         outputMode,
         "json_object",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         EnsureJsonObject(outputText, "json_object");
      }

      return outputText;
   }

   public static string NormalizeStructuredJsonOutput(string outputText)
   {
      if(string.IsNullOrWhiteSpace(outputText))
      {
         return outputText;
      }

      var trimmed = outputText.Trim();
      var match = FencedJsonRegex.Match(trimmed);

      if(!match.Success)
      {
         return trimmed;
      }

      return match.Groups["content"].Value.Trim();
   }

   private static readonly Regex FencedJsonRegex = new(
      @"^\s*```(?:json)?\s*(?<content>.*?)\s*```\s*$",
      RegexOptions.IgnoreCase |
      RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static bool IsMessageItem(JsonElement item)
   {
      return item.TryGetProperty("type", out var type) &&
         type.ValueKind == JsonValueKind.String &&
         string.Equals(
            type.GetString(),
            "message",
            StringComparison.Ordinal
         );
   }

   private static string ExtractMessageText(JsonElement item)
   {
      if(
         !item.TryGetProperty("content", out var content) ||
         content.ValueKind != JsonValueKind.Array
      )
      {
         return "";
      }

      var builder = new System.Text.StringBuilder();

      foreach(var contentItem in content.EnumerateArray())
      {
         if(
            !contentItem.TryGetProperty("text", out var text) ||
            text.ValueKind != JsonValueKind.String
         )
         {
            continue;
         }

         builder.Append(text.GetString());
      }

      return builder.ToString();
   }

   private static void EnsureJsonObject(string outputText, string mode)
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);

         if(document.RootElement.ValueKind == JsonValueKind.Object)
         {
            return;
         }
      }
      catch (JsonException exception)
      {
         throw CreateInvalidOutputException(mode, outputText, exception);
      }

      throw CreateInvalidOutputException(
         mode,
         outputText,
         new JsonException("Expected a JSON object.")
      );
   }

   private static void EnsureSchemaConformity(
      string outputText,
      string? outputSchemaJson
   )
   {
      if(string.IsNullOrWhiteSpace(outputSchemaJson))
      {
         EnsureJsonObject(outputText, "json_schema");
         return;
      }

      JsonElement schemaRoot;

      try
      {
         using var schemaDocument = JsonDocument.Parse(outputSchemaJson);
         schemaRoot = schemaDocument.RootElement.Clone();
      }
      catch (JsonException exception)
      {
         throw new InvalidOperationException(
            "AI prompt output schema is not valid JSON.",
            exception
         );
      }

      try
      {
         using var document = JsonDocument.Parse(outputText);
         ValidateSchema(document.RootElement, schemaRoot, "$");
      }
      catch (JsonException exception)
      {
         throw CreateInvalidOutputException(
            "json_schema",
            outputText,
            exception
         );
      }
   }

   private static void ValidateSchema(
      JsonElement value,
      JsonElement schema,
      string path
   )
   {
      if(!TryGetSchemaType(schema, out var schemaType))
      {
         ValidateObjectShape(value, schema, path);
         return;
      }

      switch(schemaType)
      {
         case "object":
            ValidateObjectShape(value, schema, path);
            return;
         case "string":
            if(value.ValueKind != JsonValueKind.String)
            {
               throw new JsonException(
                  $"{path} must be a JSON string."
               );
            }

            return;
         case "number":
            if(value.ValueKind != JsonValueKind.Number)
            {
               throw new JsonException(
                  $"{path} must be a JSON number."
               );
            }

            return;
         case "integer":
            if(value.ValueKind != JsonValueKind.Number ||
               !IsInteger(value))
            {
               throw new JsonException(
                  $"{path} must be a JSON integer."
               );
            }

            return;
         case "boolean":
            if(value.ValueKind != JsonValueKind.True &&
               value.ValueKind != JsonValueKind.False)
            {
               throw new JsonException(
                  $"{path} must be a JSON boolean."
               );
            }

            return;
         case "null":
            if(value.ValueKind != JsonValueKind.Null)
            {
               throw new JsonException($"{path} must be null.");
            }

            return;
         case "array":
            if(value.ValueKind != JsonValueKind.Array)
            {
               throw new JsonException($"{path} must be a JSON array.");
            }

            return;
         default:
            ValidateObjectShape(value, schema, path);
            return;
      }
   }

   private static void ValidateObjectShape(
      JsonElement value,
      JsonElement schema,
      string path
   )
   {
      if(value.ValueKind != JsonValueKind.Object)
      {
         throw new JsonException($"{path} must be a JSON object.");
      }

      var definedProperties = new HashSet<string>(
         StringComparer.Ordinal
      );

      if(
         schema.TryGetProperty("properties", out var properties) &&
         properties.ValueKind == JsonValueKind.Object
      )
      {
         foreach(var property in properties.EnumerateObject())
         {
            definedProperties.Add(property.Name);

            if(!value.TryGetProperty(property.Name, out var child))
            {
               continue;
            }

            ValidateSchema(
               child,
               property.Value,
               $"{path}.{property.Name}"
            );
         }
      }

      if(
         schema.TryGetProperty("required", out var required) &&
         required.ValueKind == JsonValueKind.Array
      )
      {
         foreach(var requiredItem in required.EnumerateArray())
         {
            if(
               requiredItem.ValueKind != JsonValueKind.String ||
               value.TryGetProperty(requiredItem.GetString()!, out _)
            )
            {
               continue;
            }

            throw new JsonException(
               $"{path} is missing required property " +
               $"'{requiredItem.GetString()}'."
            );
         }
      }

      if(
         schema.TryGetProperty("additionalProperties", out var additional) &&
         additional.ValueKind == JsonValueKind.False
      )
      {
         foreach(var property in value.EnumerateObject())
         {
            if(definedProperties.Contains(property.Name))
            {
               continue;
            }

            throw new JsonException(
               $"{path} contains unexpected property " +
               $"'{property.Name}'."
            );
         }
      }
   }

   private static bool TryGetSchemaType(
      JsonElement schema,
      out string schemaType
   )
   {
      schemaType = "";

      if(
         !schema.TryGetProperty("type", out var type) ||
         type.ValueKind != JsonValueKind.String
      )
      {
         return false;
      }

      schemaType = type.GetString() ?? "";
      return schemaType.Length > 0;
   }

   private static bool IsInteger(JsonElement value)
   {
      if(value.TryGetInt64(out _))
      {
         return true;
      }

      return value.TryGetDecimal(out var decimalValue) &&
         decimalValue == decimal.Truncate(decimalValue);
   }

   private static InvalidOperationException CreateInvalidOutputException(
      string mode,
      string outputText,
      Exception exception
   )
   {
      var preview = outputText.ReplaceLineEndings(" ").Trim();

      if(preview.Length > 240)
      {
         preview = preview[..240] + "...";
      }

      return new InvalidOperationException(
         $"AI job returned invalid {mode} output: {preview}",
         exception
      );
   }
}
