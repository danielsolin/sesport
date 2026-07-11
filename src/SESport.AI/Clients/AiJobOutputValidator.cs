using SESport.Core.AI;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.AI.Clients;

internal static class AiJobOutputValidator
{
   private static readonly string[] ParticipantListTerms =
   [
      "entry list",
      "entries",
      "participants",
      "competitors",
      "start list",
      "start lists",
      "startlist",
      "startlists",
      "start resultat",
      "players",
      "riders",
      "drivers",
      "athletes",
      "deltagare",
      "startlista"
   ];

   private static readonly string[] TeamRosterTerms =
   [
      "team roster",
      "team squad",
      "team selection",
      "roster",
      "squad",
      "lineup",
      "line up",
      "trupp",
      "laguppstallning",
      "laguppställning"
   ];

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
      var evidenceType = ReadStringProperty(root, "EvidenceType");
      var participantCount = CountArrayItems(
         root,
         "Participants",
         PrimaryCountry.LanguageName + "Participants"
      );
      var sources = ReadStringArray(root, "Sources");

      ValidateParticipationShape(
         outputText,
         participation,
         evidenceType,
         participantCount,
         sources
      );

      if(requireFetchedSources)
      {
         var sourceEvidence = ExtractSourceEvidence(toolTrace);
         ValidateSources(
            outputText,
            sources,
            evidenceType!,
            sourceEvidence
         );
         ValidateConclusionEvidence(
            outputText,
            participation!,
            evidenceType!,
            sources,
            sourceEvidence
         );
         ValidateUnknownEvidence(
            outputText,
            participation!,
            evidenceType!,
            sourceEvidence
         );
      }
   }

   private static void ValidateParticipationShape(
      string outputText,
      string? participation,
      string? evidenceType,
      int participantCount,
      IReadOnlyList<string> sources
   )
   {
      if(!IsKnownParticipationValue(participation))
      {
         throw CreateInvalidOutputException(
            "Participation must be Yes, No, or Unknown.",
            outputText
         );
      }

      if(!IsKnownEvidenceType(evidenceType))
      {
         throw CreateInvalidOutputException(
            "EvidenceType must be ParticipantList, TeamRoster, " +
            "EventInfoOnly, or SearchOnly.",
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

      if(string.Equals(
         participation,
         "Yes",
         StringComparison.Ordinal
      ) && string.Equals(
         evidenceType,
         AiParticipationEvidenceTypeIds.SearchOnly,
         StringComparison.Ordinal
      ))
      {
         throw CreateInvalidOutputException(
            "Participation Yes requires fetched page evidence.",
            outputText
         );
      }

      if(string.Equals(
         participation,
         "Unknown",
         StringComparison.Ordinal
      ) && IsStrongEvidenceType(evidenceType))
      {
         throw CreateInvalidOutputException(
            "Participation Unknown must use weak evidence.",
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
   }

   private static void ValidateSources(
      string outputText,
      IReadOnlyList<string> sources,
      string evidenceType,
      SourceEvidence sourceEvidence
   )
   {
      foreach(var source in sources)
      {
         var normalizedSource = NormalizeUrl(source);

         if(string.Equals(
            evidenceType,
            AiParticipationEvidenceTypeIds.SearchOnly,
            StringComparison.Ordinal
         ))
         {
            if(sourceEvidence.SearchSources.Contains(normalizedSource))
            {
               continue;
            }

            throw CreateInvalidOutputException(
               "SearchOnly sources must come from web_search results.",
               outputText
            );
         }

         if(sourceEvidence.FetchedSources.ContainsKey(normalizedSource))
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

   private static void ValidateConclusionEvidence(
      string outputText,
      string participation,
      string evidenceType,
      IReadOnlyList<string> sources,
      SourceEvidence sourceEvidence
   )
   {
      if(!string.Equals(participation, "No", StringComparison.Ordinal))
      {
         return;
      }

      if(!IsStrongEvidenceType(evidenceType))
      {
         throw CreateInvalidOutputException(
            "Participation No requires participant-list or team-roster " +
            "evidence.",
            outputText
         );
      }

      if(sources.Any(source => SourceMatchesEvidenceType(
         source,
         evidenceType,
         sourceEvidence
      )))
      {
         return;
      }

      throw CreateInvalidOutputException(
         "Participation No requires a fetched participant-list or " +
         "team-roster source.",
         outputText
      );
   }

   private static void ValidateUnknownEvidence(
      string outputText,
      string participation,
      string evidenceType,
      SourceEvidence sourceEvidence
   )
   {
      if(!string.Equals(participation, "Unknown", StringComparison.Ordinal) ||
         IsStrongEvidenceType(evidenceType))
      {
         return;
      }

      if(sourceEvidence.HasPrimaryCountryCheck)
      {
         return;
      }

      throw CreateInvalidOutputException(
         "Participation Unknown requires a target-country web_search or " +
         "web_find_in_page check.",
         outputText
      );
   }

   private static bool SourceMatchesEvidenceType(
      string source,
      string evidenceType,
      SourceEvidence sourceEvidence
   )
   {
      if(!sourceEvidence.FetchedSources.TryGetValue(
         NormalizeUrl(source),
         out var sourceType
      ))
      {
         return false;
      }

      return string.Equals(sourceType, evidenceType, StringComparison.Ordinal);
   }

   private static bool ContainsPrimaryCountryTerm(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return false;
      }

      var terms = new[]
      {
         PrimaryCountry.CountryName,
         PrimaryCountry.LocalDisplayName,
         PrimaryCountry.LanguageName,
         PrimaryCountry.ThreeLetterCode
      };

      return ContainsAnyEvidenceTerm(value, terms);
   }

   private static SourceEvidence ExtractSourceEvidence(JsonArray? toolTrace)
   {
      var sourceEvidence = new SourceEvidence();

      if(toolTrace is null)
      {
         return sourceEvidence;
      }

      foreach(var entry in toolTrace.OfType<JsonObject>())
      {
         var toolName = ReadString(entry, "name");

         if(string.Equals(toolName, WebToolNames.Search,
            StringComparison.Ordinal))
         {
            if(ContainsPrimaryCountryTerm(ReadString(entry, "query")))
            {
               sourceEvidence.HasPrimaryCountryCheck = true;
            }

            AddSearchSources(sourceEvidence, ReadString(entry, "result"));
            continue;
         }

         if(!string.Equals(toolName, WebToolNames.GetPage,
               StringComparison.Ordinal) &&
            !string.Equals(toolName, WebToolNames.FindInPage,
               StringComparison.Ordinal))
         {
            continue;
         }

         if(string.Equals(toolName, WebToolNames.FindInPage,
            StringComparison.Ordinal) &&
            ContainsPrimaryCountryTerm(ReadString(entry, "find")))
         {
            sourceEvidence.HasPrimaryCountryCheck = true;
         }

         AddFetchedSource(sourceEvidence, entry);
      }

      return sourceEvidence;
   }

   private static void AddSearchSources(
      SourceEvidence sourceEvidence,
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

         if(document.RootElement.ValueKind != JsonValueKind.Array)
         {
            return;
         }

         foreach(var item in document.RootElement.EnumerateArray())
         {
            AddUrl(sourceEvidence.SearchSources, ReadString(item, "url"));
         }
      }
      catch(JsonException)
      {
      }
   }

   private static void AddFetchedSource(
      SourceEvidence sourceEvidence,
      JsonObject entry
   )
   {
      var result = ReadString(entry, "result");

      if(IsFetchErrorResult(result))
      {
         return;
      }

      var evidenceType = ClassifyFetchedSource(result);

      AddFetchedUrl(
         sourceEvidence,
         ReadString(entry, "url"),
         evidenceType
      );
      AddFetchedUrl(
         sourceEvidence,
         ReadResultUrl(result),
         evidenceType
      );
   }

   private static string ClassifyFetchedSource(string? result)
   {
      var evidenceText = ExtractClassifiableEvidenceText(result);

      if(ContainsAnyEvidenceTerm(evidenceText, TeamRosterTerms))
      {
         return AiParticipationEvidenceTypeIds.TeamRoster;
      }

      if(ContainsAnyEvidenceTerm(evidenceText, ParticipantListTerms))
      {
         return AiParticipationEvidenceTypeIds.ParticipantList;
      }

      return AiParticipationEvidenceTypeIds.EventInfoOnly;
   }

   private static string ExtractClassifiableEvidenceText(string? result)
   {
      if(string.IsNullOrWhiteSpace(result))
      {
         return "";
      }

      if(TryExtractJsonEvidenceText(result, out var jsonEvidenceText))
      {
         return jsonEvidenceText;
      }

      return ExtractTextPageEvidenceText(result);
   }

   private static bool TryExtractJsonEvidenceText(
      string result,
      out string evidenceText
   )
   {
      evidenceText = "";

      try
      {
         using var document = JsonDocument.Parse(result);
         var builder = new StringBuilder();
         AppendJsonEvidenceText(builder, document.RootElement, null);
         evidenceText = builder.ToString();
         return true;
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static void AppendJsonEvidenceText(
      StringBuilder builder,
      JsonElement element,
      string? propertyName
   )
   {
      if(ShouldIgnoreEvidenceProperty(propertyName))
      {
         return;
      }

      if(element.ValueKind == JsonValueKind.Object)
      {
         foreach(var property in element.EnumerateObject())
         {
            AppendJsonEvidenceText(
               builder,
               property.Value,
               property.Name
            );
         }

         return;
      }

      if(element.ValueKind == JsonValueKind.Array)
      {
         foreach(var item in element.EnumerateArray())
         {
            AppendJsonEvidenceText(builder, item, propertyName);
         }

         return;
      }

      if(element.ValueKind == JsonValueKind.String)
      {
         builder.Append(' ');
         builder.Append(element.GetString());
      }
   }

   private static bool ShouldIgnoreEvidenceProperty(string? propertyName)
   {
      return string.Equals(propertyName, "url", StringComparison.Ordinal) ||
         string.Equals(
            propertyName,
            "reference_value",
            StringComparison.Ordinal
         ) ||
         string.Equals(
            propertyName,
            "reference_label",
            StringComparison.Ordinal
         ) ||
         string.Equals(propertyName, "published_at", StringComparison.Ordinal);
   }

   private static string ExtractTextPageEvidenceText(string result)
   {
      var builder = new StringBuilder(result.Length);
      var skippedSection = "";

      foreach(var rawLine in result.Split('\n'))
      {
         var line = rawLine.Trim();

         if(IsSkippedEvidenceSectionHeader(line))
         {
            skippedSection = line;
            continue;
         }

         if(IsKnownEvidenceSectionHeader(line))
         {
            skippedSection = "";
         }

         if(skippedSection.Length > 0 || IsIgnoredEvidenceLine(line))
         {
            continue;
         }

         builder.Append(' ');
         builder.Append(line);
      }

      return builder.ToString();
   }

   private static bool IsSkippedEvidenceSectionHeader(string line)
   {
      return string.Equals(
         line,
         "Search snippet:",
         StringComparison.Ordinal
      ) || string.Equals(
         line,
         "Relevant links:",
         StringComparison.Ordinal
      );
   }

   private static bool IsKnownEvidenceSectionHeader(string line)
   {
      return string.Equals(line, "Headings:", StringComparison.Ordinal) ||
         string.Equals(line, "Page text:", StringComparison.Ordinal) ||
         line.StartsWith("Detected rows for ", StringComparison.Ordinal) ||
         string.Equals(line, "Fetch error:", StringComparison.Ordinal);
   }

   private static bool IsIgnoredEvidenceLine(string line)
   {
      return line.StartsWith("Page URL:", StringComparison.Ordinal) ||
         line.StartsWith("URL:", StringComparison.Ordinal) ||
         line.StartsWith("Published:", StringComparison.Ordinal);
   }

   private static void AddFetchedUrl(
      SourceEvidence sourceEvidence,
      string? url,
      string evidenceType
   )
   {
      if(string.IsNullOrWhiteSpace(url))
      {
         return;
      }

      var normalizedUrl = NormalizeUrl(url);

      if(sourceEvidence.FetchedSources.TryGetValue(
         normalizedUrl,
         out var existingType
      ) && IsStrongerEvidenceType(existingType, evidenceType))
      {
         return;
      }

      sourceEvidence.FetchedSources[normalizedUrl] = evidenceType;
   }

   private static bool IsKnownParticipationValue(string? value)
   {
      return string.Equals(value, "Yes", StringComparison.Ordinal) ||
         string.Equals(value, "No", StringComparison.Ordinal) ||
         string.Equals(value, "Unknown", StringComparison.Ordinal);
   }

   private static bool IsKnownEvidenceType(string? value)
   {
      return string.Equals(
         value,
         AiParticipationEvidenceTypeIds.ParticipantList,
         StringComparison.Ordinal
      ) || string.Equals(
         value,
         AiParticipationEvidenceTypeIds.TeamRoster,
         StringComparison.Ordinal
      ) || string.Equals(
         value,
         AiParticipationEvidenceTypeIds.EventInfoOnly,
         StringComparison.Ordinal
      ) || string.Equals(
         value,
         AiParticipationEvidenceTypeIds.SearchOnly,
         StringComparison.Ordinal
      );
   }

   private static bool IsStrongEvidenceType(string? value)
   {
      return string.Equals(
         value,
         AiParticipationEvidenceTypeIds.ParticipantList,
         StringComparison.Ordinal
      ) || string.Equals(
         value,
         AiParticipationEvidenceTypeIds.TeamRoster,
         StringComparison.Ordinal
      );
   }

   private static bool IsStrongerEvidenceType(
      string existingType,
      string newType
   )
   {
      return GetEvidenceStrength(existingType) >= GetEvidenceStrength(newType);
   }

   private static int GetEvidenceStrength(string evidenceType)
   {
      return evidenceType switch
      {
         AiParticipationEvidenceTypeIds.TeamRoster => 3,
         AiParticipationEvidenceTypeIds.ParticipantList => 2,
         AiParticipationEvidenceTypeIds.EventInfoOnly => 1,
         _ => 0
      };
   }

   private static bool ContainsAnyEvidenceTerm(
      string value,
      IReadOnlyList<string> terms
   )
   {
      var normalizedValue = $" {NormalizeEvidenceText(value)} ";

      foreach(var term in terms)
      {
         if(normalizedValue.Contains(
            $" {NormalizeEvidenceText(term)} ",
            StringComparison.Ordinal
         ))
         {
            return true;
         }
      }

      return false;
   }

   private static string NormalizeEvidenceText(string value)
   {
      var builder = new StringBuilder(value.Length);

      foreach(var character in value.ToLowerInvariant())
      {
         builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
      }

      return string.Join(
         ' ',
         builder
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
      );
   }

   private static bool IsFetchErrorResult(string? result)
   {
      return result?.Contains(
         "Fetch error:",
         StringComparison.OrdinalIgnoreCase
      ) == true;
   }

   private static string? ReadResultUrl(string? result)
   {
      if(string.IsNullOrWhiteSpace(result))
      {
         return null;
      }

      if(TryReadJsonUrl(result, out var jsonUrl))
      {
         return jsonUrl;
      }

      foreach(var line in result.Split(
         '\n',
         StringSplitOptions.RemoveEmptyEntries
      ))
      {
         var trimmedLine = line.Trim();

         if(!trimmedLine.StartsWith("URL:", StringComparison.Ordinal))
         {
            continue;
         }

         return trimmedLine["URL:".Length..].Trim();
      }

      return null;
   }

   private static bool TryReadJsonUrl(
      string result,
      out string? url
   )
   {
      url = null;

      try
      {
         using var document = JsonDocument.Parse(result);
         url = ReadString(document.RootElement, "url");
         return !string.IsNullOrWhiteSpace(url);
      }
      catch(JsonException)
      {
         return false;
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

   private sealed class SourceEvidence
   {
      public HashSet<string> SearchSources { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, string> FetchedSources { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public bool HasPrimaryCountryCheck { get; set; }
   }
}
