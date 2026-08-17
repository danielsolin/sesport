using SESport.Core.Formatting;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SESport.Data;

internal static class PostgreSqlJson
{
   public static string? Normalize(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      try
      {
         value = SanitizeEscapedUnicode(
            UnicodeTextSanitizer.Sanitize(value)
         );
         using var document = JsonDocument.Parse(value);
         using var stream = new MemoryStream();

         using(var writer = new Utf8JsonWriter(stream))
         {
            WriteElement(writer, document.RootElement);
         }

         return Encoding.UTF8.GetString(stream.ToArray());
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static string SanitizeEscapedUnicode(string value)
   {
      var sanitized = new StringBuilder(value.Length);
      var inString = false;

      for(var index = 0; index < value.Length; index++)
      {
         var character = value[index];

         if(character == '"')
         {
            inString = !inString;
            sanitized.Append(character);
            continue;
         }

         if(!inString || character != '\\' || index + 1 >= value.Length)
         {
            sanitized.Append(character);
            continue;
         }

         if(value[index + 1] != 'u' ||
            !TryReadUnicodeEscape(value, index, out var codePoint))
         {
            sanitized.Append(character);
            sanitized.Append(value[++index]);
            continue;
         }

         if(codePoint == 0)
         {
            index += 5;
            continue;
         }

         if(char.IsHighSurrogate((char)codePoint))
         {
            var lowSurrogateIndex = index + 6;
            if(TryReadUnicodeEscape(
               value,
               lowSurrogateIndex,
               out var lowCodePoint
            ) &&
               char.IsLowSurrogate((char)lowCodePoint))
            {
               sanitized.Append(value, index, 12);
               index += 11;
               continue;
            }

            sanitized.Append(@"\uFFFD");
            index += 5;
            continue;
         }

         sanitized.Append(
            char.IsLowSurrogate((char)codePoint)
               ? @"\uFFFD"
               : value.Substring(index, 6)
         );
         index += 5;
      }

      return sanitized.ToString();
   }

   private static bool TryReadUnicodeEscape(
      string value,
      int index,
      out int codePoint
   )
   {
      codePoint = 0;

      return index + 5 < value.Length &&
         value[index] == '\\' &&
         value[index + 1] == 'u' &&
         int.TryParse(
            value.AsSpan(index + 2, 4),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out codePoint
         );
   }

   private static void WriteElement(
      Utf8JsonWriter writer,
      JsonElement element
   )
   {
      switch(element.ValueKind)
      {
         case JsonValueKind.Object:
            writer.WriteStartObject();
            foreach(var property in element.EnumerateObject())
            {
               writer.WritePropertyName(
                  UnicodeTextSanitizer.Sanitize(property.Name)
               );
               WriteElement(writer, property.Value);
            }
            writer.WriteEndObject();
            break;
         case JsonValueKind.Array:
            writer.WriteStartArray();
            foreach(var item in element.EnumerateArray())
            {
               WriteElement(writer, item);
            }
            writer.WriteEndArray();
            break;
         case JsonValueKind.String:
            writer.WriteStringValue(
               UnicodeTextSanitizer.Sanitize(element.GetString() ?? "")
            );
            break;
         case JsonValueKind.Number:
            writer.WriteRawValue(element.GetRawText());
            break;
         case JsonValueKind.True:
            writer.WriteBooleanValue(true);
            break;
         case JsonValueKind.False:
            writer.WriteBooleanValue(false);
            break;
         case JsonValueKind.Null:
            writer.WriteNullValue();
            break;
         default:
            throw new JsonException(
               $"Unsupported JSON value kind '{element.ValueKind}'."
            );
      }
   }
}
