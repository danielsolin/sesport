using SESport.Core.Formatting;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SESport.Core.Broadcast;

public static class BroadcastActivityPrefillBuilder
{
   public static IReadOnlyList<Guid> NormalizeBroadcastIds(
      IEnumerable<Guid> ids
   )
   {
      return ids
         .Where(id => id != Guid.Empty)
         .Distinct()
         .Take(1)
         .ToList();
   }

   public static string CreateActivityTitle(
      BroadcastActivitySource broadcast,
      IReadOnlyList<BroadcastEntityOption> entities,
      BroadcastParticipationCheck? participationCheck
   )
   {
      var organizationNames = GetSelectedOrganizationNames(
         entities,
         participationCheck
      );

      var cleanedTitle = organizationNames.Count == 0
         ? broadcast.Title.Trim()
         : RemoveRedundantOrganizationNames(
            broadcast.Title,
            organizationNames
         );

      if(string.IsNullOrWhiteSpace(cleanedTitle))
      {
         cleanedTitle = broadcast.Title.Trim();
      }

      return NormalizeShoutedTitle(cleanedTitle);
   }

   public static string CreateEvidenceComment(
      BroadcastActivitySource broadcast,
      BroadcastParticipationCheck? participationCheck
   )
   {
      var lines = new List<string>
      {
         CreateBroadcastSummary(broadcast)
      };

      if(participationCheck is null)
      {
         return string.Join(Environment.NewLine, lines);
      }

      lines.Add($"AI participation: {participationCheck.SummaryText}");

      if(participationCheck.Participants.Count > 0)
      {
         lines.Add(
            "AI participants: " +
            string.Join(", ", participationCheck.Participants)
         );
      }

      if(participationCheck.SourceUrls.Count > 0)
      {
         lines.Add("AI sources:");
         lines.AddRange(
            participationCheck.SourceUrls.Select(url => $"- {url}")
         );
      }

      return string.Join(Environment.NewLine, lines);
   }

   private static IReadOnlyList<string> GetSelectedOrganizationNames(
      IReadOnlyList<BroadcastEntityOption> entities,
      BroadcastParticipationCheck? participationCheck
   )
   {
      if(participationCheck is null ||
         participationCheck.Participants.Count == 0)
      {
         return [];
      }

      var matchedEntityIds = BroadcastEntityFilter.MatchPersonEntityIds(
         entities,
         participationCheck.Participants
      );

      return entities
         .Where(entity =>
            matchedEntityIds.Contains(entity.Id) &&
            !string.IsNullOrWhiteSpace(entity.Organization))
         .Select(entity => entity.Organization.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private static string RemoveRedundantOrganizationNames(
      string title,
      IReadOnlyCollection<string> organizationNames
   )
   {
      var normalizedTitle = NormalizeTextWithMap(title);
      var spans = new List<(int Start, int End)>();

      foreach(var organizationName in organizationNames.OrderByDescending(
         name => name.Length))
      {
         var normalizedOrganizationName = NormalizeText(organizationName);

         foreach(var pattern in CreatePatternVariants(
            normalizedOrganizationName))
         {
            foreach(var match in FindMatches(normalizedTitle.Text, pattern))
            {
               spans.Add((
                  normalizedTitle.IndexMap[match.Start],
                  normalizedTitle.IndexMap[match.End - 1] + 1
               ));
            }
         }
      }

      if(spans.Count == 0)
      {
         return title.Trim();
      }

      var mergedSpans = MergeSpans(spans);
      var cleanedTitle = RemoveSpans(title, mergedSpans);

      return CleanTitle(cleanedTitle);
   }

   private static IEnumerable<string> CreatePatternVariants(string value)
   {
      yield return value;

      var lastSpaceIndex = value.LastIndexOf(' ');

      if(lastSpaceIndex < 0)
      {
         if(value.Equals("tour", StringComparison.OrdinalIgnoreCase))
         {
            yield return "touren";
         }

         if(value.Equals("serie", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("series", StringComparison.OrdinalIgnoreCase))
         {
            yield return "serien";
         }

         yield break;
      }

      var prefix = value[..lastSpaceIndex];
      var lastWord = value[(lastSpaceIndex + 1)..];

      if(lastWord.Equals("tour", StringComparison.OrdinalIgnoreCase))
      {
         yield return $"{prefix} touren";
      }

      if(lastWord.Equals("serie", StringComparison.OrdinalIgnoreCase) ||
         lastWord.Equals("series", StringComparison.OrdinalIgnoreCase))
      {
         yield return $"{prefix} serien";
      }
   }

   private static IEnumerable<(int Start, int End)> FindMatches(
      string value,
      string pattern
   )
   {
      if(string.IsNullOrWhiteSpace(pattern))
      {
         yield break;
      }

      var regex = new Regex(
         $@"(?<!\w){Regex.Escape(pattern)}(?!\w)",
         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
      );

      foreach(Match match in regex.Matches(value))
      {
         if(match.Success)
         {
            yield return (match.Index, match.Index + match.Length);
         }
      }
   }

   private static List<(int Start, int End)> MergeSpans(
      IEnumerable<(int Start, int End)> spans
   )
   {
      var mergedSpans = new List<(int Start, int End)>();

      foreach(var span in spans
         .Where(span => span.End > span.Start)
         .OrderBy(span => span.Start)
         .ThenByDescending(span => span.End))
      {
         if(mergedSpans.Count == 0)
         {
            mergedSpans.Add(span);
            continue;
         }

         var lastSpan = mergedSpans[^1];

         if(span.Start <= lastSpan.End)
         {
            mergedSpans[^1] = (
               lastSpan.Start,
               Math.Max(lastSpan.End, span.End)
            );
            continue;
         }

         mergedSpans.Add(span);
      }

      return mergedSpans;
   }

   private static string RemoveSpans(
      string value,
      IReadOnlyList<(int Start, int End)> spans
   )
   {
      var result = new StringBuilder(value);

      foreach(var span in spans.OrderByDescending(span => span.Start))
      {
         result.Remove(span.Start, span.End - span.Start);
      }

      return result.ToString();
   }

   private static string CleanTitle(string value)
   {
      var cleaned = Regex.Replace(value, @"\s{2,}", " ");
      cleaned = Regex.Replace(cleaned, @"^\s*[,;/:\-]+\s*", string.Empty);
      cleaned = Regex.Replace(cleaned, @"\s*[,;/:\-]+\s*$", string.Empty);
      cleaned = Regex.Replace(cleaned, @"\s*,\s*", ", ");
      cleaned = Regex.Replace(cleaned, @"\s*/\s*", " / ");

      return cleaned.Trim();
   }

   private static string NormalizeShoutedTitle(string value)
   {
      var trimmed = CleanTitle(value);

      if(!LooksShouted(trimmed))
      {
         return trimmed;
      }

      var lowered = trimmed.ToLowerInvariant();
      var words = Regex.Replace(
         lowered,
         @"\b[\p{L}\p{Nd}]+\b",
         match => FormatShoutedWord(match.Value)
      );

      return CleanTitle(words);
   }

   private static bool LooksShouted(string value)
   {
      var letterCount = 0;
      var uppercaseCount = 0;

      foreach(var character in value)
      {
         if(!char.IsLetter(character))
         {
            continue;
         }

         letterCount++;

         if(char.IsUpper(character))
         {
            uppercaseCount++;
         }
      }

      return letterCount >= 4 && uppercaseCount >= letterCount * 3 / 4;
   }

   private static string FormatShoutedWord(string value)
   {
      var upper = value.ToUpperInvariant();

      if(KnownAcronyms.Contains(upper))
      {
         return upper;
      }

      return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
   }

   private static readonly ISet<string> KnownAcronyms =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
         "ATP",
         "DTM",
         "F1",
         "F2",
         "F3",
         "GT",
         "IMSA",
         "LPGA",
         "NBA",
         "NFL",
         "NHL",
         "PGA",
         "UCI",
         "UFC",
         "WEC",
         "WRC",
         "WTA"
      };

   private static NormalizedText NormalizeTextWithMap(string value)
   {
      var builder = new StringBuilder(value.Length);
      var indexMap = new List<int>(value.Length);

      for(var index = 0; index < value.Length;)
      {
         var rune = Rune.GetRuneAt(value, index);
         var runeText = rune.ToString().Normalize(NormalizationForm.FormD);

         foreach(var character in runeText)
         {
            if(CharUnicodeInfo.GetUnicodeCategory(character) ==
               UnicodeCategory.NonSpacingMark)
            {
               continue;
            }

            if(char.IsLetterOrDigit(character))
            {
               builder.Append(char.ToLowerInvariant(character));
               indexMap.Add(index);
               continue;
            }

            if(char.IsWhiteSpace(character) || char.IsPunctuation(character))
            {
               if(builder.Length > 0 && builder[^1] != ' ')
               {
                  builder.Append(' ');
                  indexMap.Add(index);
               }
            }
         }

         index += rune.Utf16SequenceLength;
      }

      while(builder.Length > 0 && builder[^1] == ' ')
      {
         builder.Length--;
         indexMap.RemoveAt(indexMap.Count - 1);
      }

      return new NormalizedText(builder.ToString(), indexMap);
   }

   private static string NormalizeText(string value)
   {
      return NormalizeTextWithMap(value).Text;
   }

   private sealed record NormalizedText(
      string Text,
      IReadOnlyList<int> IndexMap
   );

   private static string CreateBroadcastSummary(
      BroadcastActivitySource broadcast
   )
   {
      var localStart = TimeZoneHelper.ToLocal(
         broadcast.StartsAt,
         SportDay.TimeZoneId
      );
      var localEnd = TimeZoneHelper.ToLocal(
         broadcast.EndsAt,
         SportDay.TimeZoneId
      );

      return string.Join(
         " ",
         [
            string.Concat(
               localStart.ToString(
                  DateDisplay.DateTimeMinutesFormat,
                  CultureInfo.InvariantCulture
               ),
               "-",
               localEnd.ToString(
                  DateDisplay.TimeOnlyMinutesFormat,
                  CultureInfo.InvariantCulture
               )
            ),
            broadcast.ChannelName,
            broadcast.Title,
            broadcast.Description ?? string.Empty
         ]
      ).Trim();
   }
}
