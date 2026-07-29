using System.Text.Json;

using SESport.Core.Formatting;
using SESport.Core.Sources;

namespace SESport.Core.AI;

public static class ActivityParticipantAiOutputParser
{
   public static ActivityParticipantAiOutputDraft? Parse(string outputText)
   {
      if(string.IsNullOrWhiteSpace(outputText))
      {
         return null;
      }

      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.ValueKind != JsonValueKind.Object ||
            !TryGetArrayProperty(root, "participants", out var participants) ||
            !TryGetArrayProperty(
               root,
               "checked_sources",
               out var checkedSources
            ))
         {
            return null;
         }

         return new ActivityParticipantAiOutputDraft(
            ReadParticipants(participants),
            ReadSources(checkedSources)
         );
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static IReadOnlyList<ActivityParticipantAiParticipantDraft>
      ReadParticipants(JsonElement participants)
   {
      var result = new List<ActivityParticipantAiParticipantDraft>();

      foreach(var participant in participants.EnumerateArray())
      {
         if(participant.ValueKind != JsonValueKind.Object ||
            !TryGetStringProperty(participant, "name", out var name) ||
            string.IsNullOrWhiteSpace(name) ||
            !TryGetArrayProperty(participant, "sources", out var sources))
         {
            continue;
         }

         var fields = new List<ActivityParticipantAiFieldDraft>();
         foreach(var property in participant.EnumerateObject())
         {
            if(IsParticipantCoreProperty(property.Name))
            {
               continue;
            }

            fields.Add(ReadField(property));
         }

         result.Add(
            new ActivityParticipantAiParticipantDraft(
               name,
               fields,
               ReadSources(sources)
            )
         );
      }

      return result;
   }

   private static ActivityParticipantAiFieldDraft ReadField(
      JsonProperty property
   )
   {
      return new ActivityParticipantAiFieldDraft(
         property.Name,
         ReadScalarText(property.Value),
         property.Value.GetRawText()
      );
   }

   private static IReadOnlyList<SourceEvidenceDraft> ReadSources(
      JsonElement sources
   )
   {
      var result = new List<SourceEvidenceDraft>();
      var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach(var source in sources.EnumerateArray())
      {
         if(source.ValueKind != JsonValueKind.Object ||
            !TryGetStringProperty(source, "url", out var url))
         {
            continue;
         }

         if(string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
               uri.Scheme != Uri.UriSchemeHttps) ||
            !urls.Add(url))
         {
            continue;
         }

         result.Add(
            new SourceEvidenceDraft(
               url,
               ReadNullableString(source, "title"),
               ReadNullableString(source, "excerpt")
            )
         );
      }

      return result;
   }

   private static bool TryGetArrayProperty(
      JsonElement element,
      string propertyName,
      out JsonElement value
   )
   {
      if(TryGetProperty(element, propertyName, out value) &&
         value.ValueKind == JsonValueKind.Array)
      {
         return true;
      }

      value = default;
      return false;
   }

   private static bool TryGetStringProperty(
      JsonElement element,
      string propertyName,
      out string? value
   )
   {
      if(TryGetProperty(element, propertyName, out var property) &&
         property.ValueKind == JsonValueKind.String)
      {
         value = property.GetString()?.Trim();
         return true;
      }

      value = null;
      return false;
   }

   private static bool TryGetProperty(
      JsonElement element,
      string propertyName,
      out JsonElement value
   )
   {
      if(element.TryGetProperty(propertyName, out value))
      {
         return true;
      }

      foreach(var property in element.EnumerateObject())
      {
         if(ComparePropertyNames(property.Name, propertyName))
         {
            value = property.Value;
            return true;
         }
      }

      value = default;
      return false;
   }

   private static bool IsParticipantCoreProperty(string propertyName)
   {
      return string.Equals(
         propertyName,
         "name",
         StringComparison.OrdinalIgnoreCase
      ) ||
      string.Equals(
         propertyName,
         "sources",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static string? ReadNullableString(
      JsonElement element,
      string propertyName
   )
   {
      if(!TryGetProperty(element, propertyName, out var property) ||
         property.ValueKind != JsonValueKind.String)
      {
         return null;
      }

      var value = property.GetString()?.Trim();
      return string.IsNullOrWhiteSpace(value) ? null : value;
   }

   private static string? ReadScalarText(JsonElement element)
   {
      return element.ValueKind switch
      {
         JsonValueKind.String => NormalizeString(element.GetString()),
         JsonValueKind.Number => element.GetRawText(),
         JsonValueKind.True => "true",
         JsonValueKind.False => "false",
         JsonValueKind.Null => null,
         _ => null
      };
   }

   private static string? NormalizeString(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      return UnicodeTextSanitizer.Sanitize(value).Trim();
   }

   private static bool ComparePropertyNames(string left, string right)
   {
      return string.Equals(
         left,
         right,
         StringComparison.OrdinalIgnoreCase
      ) ||
      NormalizePropertyName(left) == NormalizePropertyName(right);
   }

   private static string NormalizePropertyName(string value)
   {
      var builder = new System.Text.StringBuilder(value.Length);

      foreach(var character in value)
      {
         if(char.IsLetterOrDigit(character))
         {
            builder.Append(char.ToLowerInvariant(character));
         }
      }

      return builder.ToString();
   }
}
