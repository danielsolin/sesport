using SESport.Core.Formatting;
using SESport.Core.Sources;
using System.Text.Json;

namespace SESport.Core.AI;

public static class ActivityParticipantAiOutputParser
{
   private const string NullLiteral = "null";

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
            !TryGetArrayProperty(root, "participants", out var participants))
         {
            return null;
         }

         var checkedSources = new List<SourceEvidenceDraft>();
         return new ActivityParticipantAiOutputDraft(
            ReadParticipants(participants, checkedSources),
            checkedSources
         );
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static IReadOnlyList<ActivityParticipantAiParticipantDraft>
      ReadParticipants(
         JsonElement participants,
         List<SourceEvidenceDraft> checkedSources
      )
   {
      var result = new List<ActivityParticipantAiParticipantDraft>();
      var seenSourceUrls = new HashSet<string>(
         StringComparer.OrdinalIgnoreCase
      );

      foreach(var participant in participants.EnumerateArray())
      {
         if(!TryReadParticipant(
               participant,
               out var draft,
               out var source
            ))
         {
            continue;
         }

         result.Add(draft);

         if(seenSourceUrls.Add(source.Url))
         {
            checkedSources.Add(source);
         }
      }

      return result;
   }

   private static bool TryReadParticipant(
      JsonElement participant,
      out ActivityParticipantAiParticipantDraft draft,
      out SourceEvidenceDraft source
   )
   {
      draft = null!;
      source = null!;

      if(participant.ValueKind != JsonValueKind.Object ||
         !TryGetStringProperty(participant, "name", out var name) ||
         string.IsNullOrWhiteSpace(name) ||
         !TryGetProperty(
            participant,
            ActivityParticipantAiFieldKeys.StartTime,
            out var startTime
         ) ||
         !TryGetStringProperty(
            participant,
            "source_url",
            out var sourceUrl
         ) ||
         !TryReadSourceEvidence(sourceUrl, out source))
      {
         return false;
      }

      // Some local models return the null literal as a JSON string.
      var startTimeText = ReadScalarText(startTime);
      var startTimeJson = IsNullLiteral(startTime)
         ? NullLiteral
         : startTime.GetRawText();

      draft = new ActivityParticipantAiParticipantDraft(
         name,
         [
            new ActivityParticipantAiFieldDraft(
               ActivityParticipantAiFieldKeys.StartTime,
               startTimeText,
               startTimeJson
            )
         ],
         [source]
      );

      return true;
   }

   private static bool TryReadSourceEvidence(
      string? url,
      out SourceEvidenceDraft source
   )
   {
      if(string.IsNullOrWhiteSpace(url) ||
         !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
         (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps))
      {
         source = null!;
         return false;
      }

      source = new SourceEvidenceDraft(url, null, null);
      return true;
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

      var normalized = UnicodeTextSanitizer.Sanitize(value).Trim();
      return IsNullLiteral(normalized) ? null : normalized;
   }

   private static bool IsNullLiteral(JsonElement element)
   {
      return element.ValueKind == JsonValueKind.String &&
         IsNullLiteral(
            UnicodeTextSanitizer.Sanitize(
               element.GetString() ?? string.Empty
            ).Trim()
         );
   }

   private static bool IsNullLiteral(string value)
   {
      return string.Equals(
         value,
         NullLiteral,
         StringComparison.OrdinalIgnoreCase
      );
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
