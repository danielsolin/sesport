using SESport.Core.AI;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.AI.Clients;

internal static class AiJobOutputValidator
{
   public static string Validate(
      string outputText,
      AiJobDefinition job,
      bool requireFetchedSources,
      JsonArray? toolTrace = null
   )
   {
      if(!string.Equals(
         job.Id,
         AiJobIds.DecidePrimaryCountryParticipation,
         StringComparison.Ordinal
      ))
      {
         return outputText;
      }

      ValidateParticipationOutput(
         outputText,
         requireFetchedSources,
         toolTrace
      );
      return outputText;
   }

   private static void ValidateParticipationOutput(
      string outputText,
      bool requireFetchedSources,
      JsonArray? toolTrace
   )
   {
      using var document = JsonDocument.Parse(outputText);
      var root = document.RootElement;

      if(root.ValueKind != JsonValueKind.Object)
      {
         throw CreateInvalidOutputException(
            "Expected a JSON object.",
            outputText
         );
      }

      var participation = ReadStringProperty(
         root,
         "Participation",
         PrimaryCountry.LanguageName + "Participation"
      );
      var participantCount = CountArrayItems(
         root,
         "Participants",
         PrimaryCountry.LanguageName + "Participants"
      );
      var sources = ReadStringArray(root, "Sources");

      if(!IsKnownParticipationValue(participation))
      {
         throw CreateInvalidOutputException(
            "Participation must be Yes, No, or Unknown.",
            outputText
         );
      }

      if(string.Equals(participation, "Yes", StringComparison.Ordinal) &&
         participantCount == 0)
      {
         throw CreateInvalidOutputException(
            "Participation Yes requires at least one participant.",
            outputText
         );
      }

      if(sources.Count == 0)
      {
         throw CreateInvalidOutputException(
            "Participation output requires at least one source URL.",
            outputText
         );
      }

      if(requireFetchedSources)
      {
         ValidateFetchedSources(outputText, sources, toolTrace);
      }
   }

   private static void ValidateFetchedSources(
      string outputText,
      IReadOnlyList<string> sources,
      JsonArray? toolTrace
   )
   {
      var fetchedSources = ExtractFetchedSources(toolTrace);

      if(fetchedSources.Count == 0)
      {
         throw CreateInvalidOutputException(
            "Sources must come from fetched pages.",
            outputText
         );
      }

      foreach(var source in sources)
      {
         if(fetchedSources.Contains(NormalizeUrl(source)))
         {
            continue;
         }

         throw CreateInvalidOutputException(
            "Sources must only include URLs fetched with web_get_page or " +
            "web_find_in_page.",
            outputText
         );
      }
   }

   private static HashSet<string> ExtractFetchedSources(JsonArray? toolTrace)
   {
      var fetchedSources = new HashSet<string>(
         StringComparer.OrdinalIgnoreCase
      );

      if(toolTrace is null)
      {
         return fetchedSources;
      }

      foreach(var entry in toolTrace.OfType<JsonObject>())
      {
         var toolName = ReadString(entry, "name");

         if(!string.Equals(toolName, WebToolNames.GetPage,
               StringComparison.Ordinal) &&
            !string.Equals(toolName, WebToolNames.FindInPage,
               StringComparison.Ordinal))
         {
            continue;
         }

         var result = ReadString(entry, "result");

         if(IsFetchErrorResult(result))
         {
            continue;
         }

         AddUrl(fetchedSources, ReadString(entry, "url"));
         AddResultUrl(fetchedSources, result);
      }

      return fetchedSources;
   }

   private static bool IsKnownParticipationValue(string? value)
   {
      return string.Equals(value, "Yes", StringComparison.Ordinal) ||
         string.Equals(value, "No", StringComparison.Ordinal) ||
         string.Equals(value, "Unknown", StringComparison.Ordinal);
   }

   private static bool IsFetchErrorResult(string? result)
   {
      return result?.Contains(
         "Fetch error:",
         StringComparison.OrdinalIgnoreCase
      ) == true;
   }

   private static void AddResultUrl(
      ISet<string> urls,
      string? resultJson
   )
   {
      if(string.IsNullOrWhiteSpace(resultJson))
      {
         return;
      }

      try
      {
         using var document = JsonDocument.Parse(resultJson);
         AddUrl(urls, ReadString(document.RootElement, "url"));
      }
      catch(JsonException)
      {
      }
   }

   private static void AddUrl(ISet<string> urls, string? url)
   {
      if(string.IsNullOrWhiteSpace(url))
      {
         return;
      }

      urls.Add(NormalizeUrl(url));
   }

   private static string? ReadStringProperty(
      JsonElement element,
      params string[] propertyNames
   )
   {
      foreach(var propertyName in propertyNames)
      {
         var value = ReadString(element, propertyName);

         if(!string.IsNullOrWhiteSpace(value))
         {
            return value;
         }
      }

      return null;
   }

   private static string? ReadString(JsonObject value, string propertyName)
   {
      if(!value.TryGetPropertyValue(propertyName, out var node) ||
         node is not JsonValue jsonValue ||
         !jsonValue.TryGetValue<string>(out var text))
      {
         return null;
      }

      return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
   }

   private static string? ReadString(
      JsonElement element,
      string propertyName
   )
   {
      if(!element.TryGetProperty(propertyName, out var property) ||
         property.ValueKind != JsonValueKind.String)
      {
         return null;
      }

      var value = property.GetString();
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
   }

   private static int CountArrayItems(
      JsonElement element,
      params string[] propertyNames
   )
   {
      foreach(var propertyName in propertyNames)
      {
         if(!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
         {
            continue;
         }

         return property.GetArrayLength();
      }

      return 0;
   }

   private static IReadOnlyList<string> ReadStringArray(
      JsonElement element,
      string propertyName
   )
   {
      if(!element.TryGetProperty(propertyName, out var property) ||
         property.ValueKind != JsonValueKind.Array)
      {
         return [];
      }

      var values = new List<string>();

      foreach(var item in property.EnumerateArray())
      {
         if(item.ValueKind != JsonValueKind.String)
         {
            continue;
         }

         var value = item.GetString();

         if(!string.IsNullOrWhiteSpace(value))
         {
            values.Add(value.Trim());
         }
      }

      return values;
   }

   private static string NormalizeUrl(string url)
   {
      var trimmed = url.Trim();

      if(!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
      {
         return trimmed;
      }

      var builder = new UriBuilder(uri)
      {
         Fragment = "",
         Host = uri.Host.ToLowerInvariant(),
         Scheme = uri.Scheme.ToLowerInvariant()
      };

      var normalized = builder.Uri.AbsoluteUri.TrimEnd('/');
      return normalized.Length == 0 ? trimmed : normalized;
   }

   private static InvalidOperationException CreateInvalidOutputException(
      string reason,
      string outputText
   )
   {
      var preview = outputText.ReplaceLineEndings(" ").Trim();

      if(preview.Length > 240)
      {
         preview = preview[..240] + "...";
      }

      return new InvalidOperationException(
         $"AI job returned invalid json_schema output: {preview}. {reason}"
      );
   }
}
