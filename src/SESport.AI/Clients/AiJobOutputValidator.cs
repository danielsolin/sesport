using SESport.Core.AI;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SESport.AI.Clients;

internal static class AiJobOutputValidator
{
   private const int MinimumParticipantListRowCount = 3;
   private static readonly Regex UrlRegex = new(
      @"https?://[^\s<>""]+",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );

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

   public static IReadOnlyList<string> ReadParticipantNames(
      string outputText
   )
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);

         if(document.RootElement.ValueKind != JsonValueKind.Object)
         {
            return [];
         }

         return ReadParticipantEvidenceArray(
            document.RootElement,
            ReadStringProperty(document.RootElement, "EvidenceType")
         )
            .Select(participant => participant.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
      }
      catch(JsonException)
      {
         return [];
      }
   }

   public static void ValidateRetainedParticipants(
      string outputText,
      IReadOnlySet<string> retainedParticipantNames
   )
   {
      if(retainedParticipantNames.Count == 0)
      {
         return;
      }

      var reportedNames = ReadParticipantNames(outputText)
         .Select(NormalizeEvidenceText)
         .ToHashSet(StringComparer.Ordinal);
      var removedNames = retainedParticipantNames
         .Where(name => !reportedNames.Contains(NormalizeEvidenceText(name)))
         .ToArray();

      if(removedNames.Length == 0)
      {
         return;
      }

      throw CreateInvalidOutputException(
         "Final report correction must preserve previously reported " +
         $"participants: {string.Join(", ", removedNames)}.",
         outputText
      );
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
      var legacyEvidenceType = ReadStringProperty(root, "EvidenceType");
      var participantEvidence = ReadParticipantEvidenceArray(
         root,
         legacyEvidenceType
      );
      var checkedSources = ReadSourceEvidenceArray(
         root,
         "CheckedSources",
         legacyEvidenceType
      );

      if(checkedSources.Count == 0)
      {
         checkedSources = ReadSourceEvidenceArray(
            root,
            "Sources",
            legacyEvidenceType
         );
      }

      ValidateParticipationShape(
         outputText,
         participation,
         participantEvidence,
         checkedSources
      );

      if(requireFetchedSources)
      {
         var sourceEvidence = ExtractSourceEvidence(toolTrace);
         ValidateParticipantSources(
            outputText,
            participantEvidence,
            sourceEvidence
         );
         ValidateCheckedSources(
            outputText,
            checkedSources,
            sourceEvidence
         );
         ValidateConclusionEvidence(
            outputText,
            participation!,
            checkedSources
         );
         ValidateUnknownEvidence(
            outputText,
            participation!,
            checkedSources,
            sourceEvidence
         );
      }
   }

   private static void ValidateParticipationShape(
      string outputText,
      string? participation,
      IReadOnlyList<ParticipantEvidence> participants,
      IReadOnlyList<SourceEvidenceReference> checkedSources
   )
   {
      if(!IsKnownParticipationValue(participation))
      {
         throw CreateInvalidOutputException(
            "Participation must be Yes, No, or Unknown.",
            outputText
         );
      }

      ValidateSourceEvidenceTypes(
         outputText,
         participants.SelectMany(participant => participant.Sources)
            .Concat(checkedSources)
      );

      foreach(var participant in participants)
      {
         if(string.IsNullOrWhiteSpace(participant.Name))
         {
            throw CreateInvalidOutputException(
               "Participant Name is required.",
               outputText
            );
         }

         if(participant.Sources.Count == 0)
         {
            throw CreateInvalidOutputException(
               "Each participant requires at least one source.",
               outputText
            );
         }

         if(participant.Sources.Any(source =>
            !IsParticipantEvidenceType(source.EvidenceType)))
         {
            throw CreateInvalidOutputException(
               "Participant sources must use participant evidence.",
               outputText
            );
         }
      }

      if(string.Equals(participation, "Yes", StringComparison.Ordinal) &&
         participants.Count == 0)
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
      ))
      {
         return;
      }

      if(participants.Count > 0)
      {
         throw CreateInvalidOutputException(
            "Participation No or Unknown requires no participants.",
            outputText
         );
      }

      if(checkedSources.Count == 0)
      {
         throw CreateInvalidOutputException(
            "Participation No or Unknown requires checked sources.",
            outputText
         );
      }

      if(string.Equals(participation, "No", StringComparison.Ordinal) &&
         !checkedSources.Any(source =>
            IsAbsenceEvidenceType(source.EvidenceType)))
      {
         throw CreateInvalidOutputException(
            "Participation No requires participant-list or team-roster " +
            "evidence.",
            outputText
         );
      }

      if(string.Equals(participation, "Unknown", StringComparison.Ordinal) &&
         checkedSources.Any(source =>
            IsConclusiveEvidenceType(source.EvidenceType)))
      {
         throw CreateInvalidOutputException(
            "Participation Unknown must use weak evidence.",
            outputText
         );
      }
   }

   private static void ValidateSourceEvidenceTypes(
      string outputText,
      IEnumerable<SourceEvidenceReference> sources
   )
   {
      foreach(var source in sources)
      {
         if(string.IsNullOrWhiteSpace(source.Url))
         {
            throw CreateInvalidOutputException(
               "Source Url is required.",
               outputText
            );
         }

         if(!IsKnownEvidenceType(source.EvidenceType))
         {
            throw CreateInvalidOutputException(
               "Source EvidenceType must be ParticipantList, " +
               "ParticipantMention, TeamRoster, EventInfoOnly, or " +
               "SearchOnly.",
               outputText
            );
         }
      }
   }

   private static bool IsParticipantEvidenceType(string evidenceType)
   {
      return string.Equals(
         evidenceType,
         AiParticipationEvidenceTypeIds.ParticipantList,
         StringComparison.Ordinal
      ) || string.Equals(
         evidenceType,
         AiParticipationEvidenceTypeIds.ParticipantMention,
         StringComparison.Ordinal
      ) || string.Equals(
         evidenceType,
         AiParticipationEvidenceTypeIds.TeamRoster,
         StringComparison.Ordinal
      );
   }

   private static void ValidateParticipantSources(
      string outputText,
      IReadOnlyList<ParticipantEvidence> participants,
      SourceEvidence sourceEvidence
   )
   {
      foreach(var participant in participants)
      {
         foreach(var source in participant.Sources)
         {
            ValidateParticipantSource(
               outputText,
               participant.Name,
               source,
               sourceEvidence
            );
         }
      }
   }

   private static void ValidateParticipantSource(
      string outputText,
      string participantName,
      SourceEvidenceReference source,
      SourceEvidence sourceEvidence
   )
   {
      if(!sourceEvidence.FetchedSources.TryGetValue(
         NormalizeUrl(source.Url),
         out var fetchedSource
      ))
      {
         throw CreateInvalidOutputException(
            "Participant sources must be fetched with web_get_page or " +
            "web_find_in_page.",
            outputText
         );
      }

      if(string.Equals(
         source.EvidenceType,
         AiParticipationEvidenceTypeIds.ParticipantMention,
         StringComparison.Ordinal
      ))
      {
         if(ContainsParticipantMention(
            fetchedSource.EvidenceText,
            participantName
         ))
         {
            return;
         }

         throw CreateInvalidOutputException(
            "ParticipantMention source must name the participant and " +
            "target country.",
            outputText
         );
      }

      if(!string.Equals(
         fetchedSource.EvidenceType,
         source.EvidenceType,
         StringComparison.Ordinal
      ))
      {
         throw CreateInvalidOutputException(
            "Participant source EvidenceType must match fetched source.",
            outputText
         );
      }

      if(ContainsAnyEvidenceTerm(fetchedSource.EvidenceText, [participantName]))
      {
         return;
      }

      throw CreateInvalidOutputException(
         "Participant source must name the participant.",
         outputText
      );
   }

   private static void ValidateCheckedSources(
      string outputText,
      IReadOnlyList<SourceEvidenceReference> sources,
      SourceEvidence sourceEvidence
   )
   {
      foreach(var source in sources)
      {
         ValidateCheckedSource(outputText, source, sourceEvidence);
      }
   }

   private static void ValidateCheckedSource(
      string outputText,
      SourceEvidenceReference source,
      SourceEvidence sourceEvidence
   )
   {
      if(string.Equals(
         source.EvidenceType,
         AiParticipationEvidenceTypeIds.SearchOnly,
         StringComparison.Ordinal
      ))
      {
         if(sourceEvidence.SearchSources.Contains(NormalizeUrl(source.Url)))
         {
            return;
         }

         throw CreateInvalidOutputException(
            "SearchOnly sources must come from web_search results.",
            outputText
         );
      }

      if(!sourceEvidence.FetchedSources.TryGetValue(
         NormalizeUrl(source.Url),
         out var fetchedSource
      ))
      {
         throw CreateInvalidOutputException(
            "Checked sources must be fetched with web_get_page or " +
            "web_find_in_page.",
            outputText
         );
      }

      if(string.Equals(
         fetchedSource.EvidenceType,
         source.EvidenceType,
         StringComparison.Ordinal
      ))
      {
         return;
      }

      throw CreateInvalidOutputException(
         "Checked source EvidenceType must match fetched source.",
         outputText
      );
   }

   private static void ValidateConclusionEvidence(
      string outputText,
      string participation,
      IReadOnlyList<SourceEvidenceReference> checkedSources
   )
   {
      if(!string.Equals(participation, "No", StringComparison.Ordinal))
      {
         return;
      }

      if(checkedSources.Any(source =>
         IsAbsenceEvidenceType(source.EvidenceType)))
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
      IReadOnlyList<SourceEvidenceReference> checkedSources,
      SourceEvidence sourceEvidence
   )
   {
      if(!string.Equals(participation, "Unknown", StringComparison.Ordinal))
      {
         return;
      }

      if(!sourceEvidence.HasPrimaryCountryCheck)
      {
         throw CreateInvalidOutputException(
            "Participation Unknown requires a target-country web_search or " +
            "web_find_in_page check.",
            outputText
         );
      }

      var checkedSourceUrls = checkedSources
         .Select(source => NormalizeUrl(source.Url))
         .ToHashSet(StringComparer.OrdinalIgnoreCase);

      foreach(var source in sourceEvidence.FetchedSources)
      {
         if(!ShouldCheckUnfollowedParticipantListLeads(
            source.Key,
            source.Value,
            checkedSourceUrls,
            checkedSources
         ))
         {
            continue;
         }

         if(HasUnfetchedParticipantListLead(source.Value, sourceEvidence))
         {
            throw CreateInvalidOutputException(
               "Participation Unknown must follow participant-list links " +
               "before returning.",
               outputText
            );
         }
      }
   }

   private static bool ShouldCheckUnfollowedParticipantListLeads(
      string sourceUrl,
      FetchedSourceEvidence fetchedSource,
      HashSet<string> checkedSourceUrls,
      IReadOnlyList<SourceEvidenceReference> checkedSources
   )
   {
      if(!fetchedSource.HasParticipantListLead)
      {
         return false;
      }

      return checkedSourceUrls.Contains(sourceUrl) ||
         checkedSources.Any(source => string.Equals(
            source.EvidenceType,
            AiParticipationEvidenceTypeIds.SearchOnly,
            StringComparison.Ordinal
         ));
   }

   private static bool HasUnfetchedParticipantListLead(
      FetchedSourceEvidence fetchedSource,
      SourceEvidence sourceEvidence
   )
   {
      if(fetchedSource.ParticipantListLeadUrls.Count == 0)
      {
         return true;
      }

      foreach(var url in fetchedSource.ParticipantListLeadUrls)
      {
         if(!sourceEvidence.FetchedSources.ContainsKey(NormalizeUrl(url)))
         {
            return true;
         }
      }

      return false;
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

      var evidenceLines = ExtractClassifiableEvidenceLines(result);
      var evidenceText = string.Join(' ', evidenceLines);
      var participantListLeadUrls = ExtractParticipantListLeadUrls(result);
      var evidenceType = ClassifyFetchedSource(evidenceLines, evidenceText);
      var fetchedSource = new FetchedSourceEvidence(
         evidenceType,
         evidenceText,
         IsParticipantListIndexPage(evidenceText) ||
            participantListLeadUrls.Count > 0,
         participantListLeadUrls
      );

      AddFetchedUrl(
         sourceEvidence,
         ReadString(entry, "url"),
         fetchedSource
      );
      AddFetchedUrl(
         sourceEvidence,
         ReadResultUrl(result),
         fetchedSource
      );
   }

   private static string ClassifyFetchedSource(
      IReadOnlyList<string> evidenceLines,
      string evidenceText
   )
   {
      if(ContainsAnyEvidenceTerm(evidenceText, TeamRosterTerms) &&
         HasParticipantListRows(evidenceLines))
      {
         return AiParticipationEvidenceTypeIds.TeamRoster;
      }

      if(IsParticipantListIndexPage(evidenceText))
      {
         return AiParticipationEvidenceTypeIds.EventInfoOnly;
      }

      if(ContainsAnyEvidenceTerm(evidenceText, ParticipantListTerms) &&
         HasParticipantListRows(evidenceLines))
      {
         return AiParticipationEvidenceTypeIds.ParticipantList;
      }

      return AiParticipationEvidenceTypeIds.EventInfoOnly;
   }

   private static bool ContainsParticipantMention(
      string evidenceText,
      string participantName
   )
   {
      if(string.IsNullOrWhiteSpace(participantName) ||
         !ContainsPrimaryCountryTerm(evidenceText))
      {
         return false;
      }

      return ContainsParticipantName(evidenceText, participantName);
   }

   private static bool ContainsParticipantName(
      string evidenceText,
      string participantName
   )
   {
      if(ContainsAnyEvidenceTerm(evidenceText, [participantName]))
      {
         return true;
      }

      var normalizedEvidence = $" {NormalizeEvidenceText(evidenceText)} ";
      var nameTokens = NormalizeEvidenceText(participantName)
         .Split(' ', StringSplitOptions.RemoveEmptyEntries);

      return nameTokens.Length > 1 && nameTokens.All(token =>
         normalizedEvidence.Contains($" {token} ", StringComparison.Ordinal)
      );
   }

   private static bool IsParticipantListIndexPage(string evidenceText)
   {
      var normalizedText = NormalizeEvidenceText(evidenceText);

      return normalizedText.Contains(
         "start lists pdf",
         StringComparison.Ordinal
      ) || normalizedText.Contains(
         "event start lists",
         StringComparison.Ordinal
      ) || normalizedText.Contains(
         "view the final start lists below",
         StringComparison.Ordinal
      );
   }

   private static IReadOnlyList<string> ExtractParticipantListLeadUrls(
      string? result
   )
   {
      if(string.IsNullOrWhiteSpace(result))
      {
         return [];
      }

      var urls = new List<string>();
      var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      var insideRelevantLinkSection = false;

      foreach(var rawLine in result.Split('\n'))
      {
         var line = rawLine.Trim();

         if(IsRelevantLinkSectionHeader(line))
         {
            insideRelevantLinkSection = true;
            continue;
         }

         if(IsKnownEvidenceSectionHeader(line))
         {
            insideRelevantLinkSection = false;
            continue;
         }

         if(!insideRelevantLinkSection)
         {
            continue;
         }

         AddUrlsFromLine(line, urls, seenUrls);
      }

      return urls;
   }

   private static void AddUrlsFromLine(
      string line,
      List<string> urls,
      HashSet<string> seenUrls
   )
   {
      foreach(Match match in UrlRegex.Matches(line))
      {
         var url = match.Value.TrimEnd('.', ',', ';', ')', ']');

         if(seenUrls.Add(NormalizeUrl(url)))
         {
            urls.Add(url);
         }
      }
   }

   private static IReadOnlyList<string> ExtractClassifiableEvidenceLines(
      string? result
   )
   {
      if(string.IsNullOrWhiteSpace(result))
      {
         return [];
      }

      if(TryExtractJsonEvidenceLines(result, out var jsonEvidenceLines))
      {
         return jsonEvidenceLines;
      }

      return ExtractTextPageEvidenceLines(result);
   }

   private static bool TryExtractJsonEvidenceLines(
      string result,
      out IReadOnlyList<string> evidenceLines
   )
   {
      evidenceLines = [];

      try
      {
         using var document = JsonDocument.Parse(result);
         var lines = new List<string>();
         AppendJsonEvidenceLines(lines, document.RootElement, null);
         evidenceLines = lines;
         return true;
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static void AppendJsonEvidenceLines(
      List<string> lines,
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
            AppendJsonEvidenceLines(
               lines,
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
            AppendJsonEvidenceLines(lines, item, propertyName);
         }

         return;
      }

      if(element.ValueKind == JsonValueKind.String)
      {
         AddEvidenceLines(lines, element.GetString());
      }
   }

   private static void AddEvidenceLines(
      List<string> lines,
      string? value
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return;
      }

      foreach(var line in value.Split('\n'))
      {
         var trimmedLine = line.Trim();

         if(!string.IsNullOrWhiteSpace(trimmedLine))
         {
            lines.Add(trimmedLine);
         }
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

   private static IReadOnlyList<string> ExtractTextPageEvidenceLines(
      string result
   )
   {
      var lines = new List<string>();
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
            continue;
         }

         if(skippedSection.Length > 0 || IsIgnoredEvidenceLine(line))
         {
            continue;
         }

         if(!string.IsNullOrWhiteSpace(line))
         {
            lines.Add(line);
         }
      }

      return lines;
   }

   private static bool HasParticipantListRows(
      IReadOnlyList<string> lines
   )
   {
      var rowCount = 0;

      foreach(var line in lines)
      {
         if(!IsParticipantListRow(line))
         {
            continue;
         }

         rowCount++;

         if(rowCount >= MinimumParticipantListRowCount)
         {
            return true;
         }
      }

      return false;
   }

   private static bool IsParticipantListRow(string line)
   {
      var trimmedLine = line.Trim();

      if(trimmedLine.Length == 0 ||
         IsParticipantListIndexLine(trimmedLine))
      {
         return false;
      }

      if(trimmedLine.Contains(" | ", StringComparison.Ordinal))
      {
         return ContainsLetter(trimmedLine);
      }

      if(ContainsCountryCodeLikeToken(trimmedLine) &&
         CountWordLikeTokens(trimmedLine) >= 3)
      {
         return true;
      }

      if(ContainsSentencePunctuation(trimmedLine))
      {
         return false;
      }

      if(StartsWithDigit(trimmedLine) && CountWordLikeTokens(trimmedLine) >= 2)
      {
         return true;
      }

      return CountWordLikeTokens(trimmedLine) is >= 2 and <= 5;
   }

   private static bool ContainsCountryCodeLikeToken(string line)
   {
      foreach(var rawToken in line.Split(
         ' ',
         StringSplitOptions.RemoveEmptyEntries
      ))
      {
         var token = rawToken.Trim(',', ';', ':', '(', ')', '[', ']');

         if(token.Length == 3 && token.All(character =>
            char.IsLetter(character) && char.IsUpper(character)))
         {
            return true;
         }
      }

      return false;
   }

   private static bool IsParticipantListIndexLine(string line)
   {
      var normalizedLine = NormalizeEvidenceText(line);

      return normalizedLine.Contains(
         "start list",
         StringComparison.Ordinal
      ) || normalizedLine.Contains(
         "start lists pdf",
         StringComparison.Ordinal
      ) || normalizedLine.Contains(
         "event start lists",
         StringComparison.Ordinal
      ) || normalizedLine.Contains(
         "view the final start lists below",
         StringComparison.Ordinal
      ) || normalizedLine.Equals("start lists", StringComparison.Ordinal) ||
         normalizedLine.Equals("results", StringComparison.Ordinal) ||
         normalizedLine.Equals("share", StringComparison.Ordinal) ||
         normalizedLine.StartsWith("posted by ", StringComparison.Ordinal) ||
         normalizedLine.StartsWith("time event ", StringComparison.Ordinal) ||
         StartsWithClockTime(line);
   }

   private static bool StartsWithClockTime(string value)
   {
      return value.Length >= 5 &&
         char.IsDigit(value[0]) &&
         char.IsDigit(value[1]) &&
         value[2] == ':' &&
         char.IsDigit(value[3]) &&
         char.IsDigit(value[4]);
   }

   private static bool ContainsSentencePunctuation(string value)
   {
      return value.Contains('.', StringComparison.Ordinal) ||
         value.Contains('?', StringComparison.Ordinal) ||
         value.Contains('!', StringComparison.Ordinal);
   }

   private static bool ContainsLetter(string value)
   {
      return value.Any(char.IsLetter);
   }

   private static bool StartsWithDigit(string value)
   {
      return value.Length > 0 && char.IsDigit(value[0]);
   }

   private static int CountWordLikeTokens(string value)
   {
      return value
         .Split(
            [' ', '\t', '|', ',', ';', ':', '-', '–', '—', '/', '(', ')'],
            StringSplitOptions.RemoveEmptyEntries
         )
         .Count(token => token.Any(char.IsLetter));
   }

   private static bool IsSkippedEvidenceSectionHeader(string line)
   {
      return string.Equals(
         line,
         "Search snippet:",
         StringComparison.Ordinal
      ) || string.Equals(
         line,
         "PDF links:",
         StringComparison.Ordinal
      ) || string.Equals(
         line,
         "Relevant links:",
         StringComparison.Ordinal
      );
   }

   private static bool IsRelevantLinkSectionHeader(string line)
   {
      return string.Equals(
         line,
         "PDF links:",
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
      FetchedSourceEvidence fetchedSource
   )
   {
      if(string.IsNullOrWhiteSpace(url))
      {
         return;
      }

      var normalizedUrl = NormalizeUrl(url);

      if(sourceEvidence.FetchedSources.TryGetValue(
         normalizedUrl,
         out var existingSource
      ) && IsStrongerEvidenceType(
         existingSource.EvidenceType,
         fetchedSource.EvidenceType
      ))
      {
         return;
      }

      sourceEvidence.FetchedSources[normalizedUrl] = fetchedSource;
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
         AiParticipationEvidenceTypeIds.ParticipantMention,
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

   private static bool IsConclusiveEvidenceType(string? value)
   {
      return string.Equals(
         value,
         AiParticipationEvidenceTypeIds.ParticipantList,
         StringComparison.Ordinal
      ) || string.Equals(
         value,
         AiParticipationEvidenceTypeIds.ParticipantMention,
         StringComparison.Ordinal
      ) || string.Equals(
         value,
         AiParticipationEvidenceTypeIds.TeamRoster,
         StringComparison.Ordinal
      );
   }

   private static bool IsAbsenceEvidenceType(string? value)
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
         AiParticipationEvidenceTypeIds.TeamRoster => 4,
         AiParticipationEvidenceTypeIds.ParticipantList => 3,
         AiParticipationEvidenceTypeIds.ParticipantMention => 2,
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

   private static bool TryGetArrayProperty(
      JsonElement element,
      string propertyName,
      string fallbackPropertyName,
      out JsonElement property
   )
   {
      if(element.TryGetProperty(propertyName, out property) &&
         property.ValueKind == JsonValueKind.Array)
      {
         return true;
      }

      if(element.TryGetProperty(fallbackPropertyName, out property) &&
         property.ValueKind == JsonValueKind.Array)
      {
         return true;
      }

      property = default;
      return false;
   }

   private static IReadOnlyList<ParticipantEvidence>
      ReadParticipantEvidenceArray(
         JsonElement element,
         string? legacyEvidenceType
      )
   {
      if(!TryGetArrayProperty(
         element,
         "Participants",
         PrimaryCountry.LanguageName + "Participants",
         out var property
      ))
      {
         return [];
      }

      var participants = new List<ParticipantEvidence>();

      foreach(var item in property.EnumerateArray())
      {
         if(item.ValueKind == JsonValueKind.String)
         {
            var legacyName = item.GetString();

            if(!string.IsNullOrWhiteSpace(legacyName))
            {
               participants.Add(
                  new ParticipantEvidence(legacyName.Trim(), [])
               );
            }

            continue;
         }

         if(item.ValueKind != JsonValueKind.Object)
         {
            continue;
         }

         var name = ReadString(item, "Name") ?? "";
         var sources = ReadSourceEvidenceArray(
            item,
            "Sources",
            legacyEvidenceType
         );

         participants.Add(new ParticipantEvidence(name, sources));
      }

      return participants;
   }

   private static IReadOnlyList<SourceEvidenceReference>
      ReadSourceEvidenceArray(
         JsonElement element,
         string propertyName,
         string? legacyEvidenceType
      )
   {
      if(!element.TryGetProperty(propertyName, out var property) ||
         property.ValueKind != JsonValueKind.Array)
      {
         return [];
      }

      var sources = new List<SourceEvidenceReference>();

      foreach(var item in property.EnumerateArray())
      {
         if(item.ValueKind == JsonValueKind.String)
         {
            if(!string.IsNullOrWhiteSpace(legacyEvidenceType))
            {
               sources.Add(
                  new SourceEvidenceReference(
                     item.GetString() ?? "",
                     legacyEvidenceType
                  )
               );
            }

            continue;
         }

         if(item.ValueKind != JsonValueKind.Object)
         {
            continue;
         }

         sources.Add(
            new SourceEvidenceReference(
               ReadString(item, "Url") ?? "",
               ReadString(item, "EvidenceType") ?? ""
            )
         );
      }

      return sources;
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

      return new AiJobOutputValidationException(
         $"AI job returned invalid json_schema output: {preview}. {reason}"
      );
   }

   private sealed class SourceEvidence
   {
      public HashSet<string> SearchSources { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, FetchedSourceEvidence> FetchedSources { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public bool HasPrimaryCountryCheck { get; set; }
   }

   private sealed record FetchedSourceEvidence(
      string EvidenceType,
      string EvidenceText,
      bool HasParticipantListLead,
      IReadOnlyList<string> ParticipantListLeadUrls
   );

   private sealed record ParticipantEvidence(
      string Name,
      IReadOnlyList<SourceEvidenceReference> Sources
   );

   private sealed record SourceEvidenceReference(
      string Url,
      string EvidenceType
   );
}
