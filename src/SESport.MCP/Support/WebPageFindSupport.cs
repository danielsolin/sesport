using SESport.Core.Formatting;

using System.Text.RegularExpressions;

namespace SESport.MCP.Support;

internal static class WebPageFindSupport
{
   internal static IReadOnlyList<PageMatch> FindPageMatches(
      WebPageContent pageContent,
      string find
   )
   {
      var matches = new List<PageMatch>();
      var seenSnippets = new HashSet<string>(StringComparer.Ordinal);
      var searchText = GetPageSearchText(pageContent);

      foreach(var snippet in ExtractTextSnippets(searchText, find))
      {
         AddSnippetMatch(matches, seenSnippets, snippet, find);
      }

      return matches;
   }

   internal static IReadOnlyList<string> ExtractMatchingCountryEntries(
      string text,
      string find,
      int maxEntries = WebPageFetchDefaults.MaxFindInPageSnippetCount
   )
   {
      if(string.IsNullOrWhiteSpace(text) ||
         string.IsNullOrWhiteSpace(find) ||
         !IsPrimaryCountryFind(find))
      {
         return [];
      }

      var lines = text.ReplaceLineEndings("\n").Split(
         '\n',
         StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries
      );
      var countryFindTerm = GetPrimaryCountryTerms()
         .FirstOrDefault(term =>
            BuildFindTermRegex([term]).IsMatch(find)
         );
      var pattern = BuildFindTermRegex(
         [countryFindTerm ?? find]
      );
      var entries = new List<string>();
      var seenEntries = new HashSet<string>(
         StringComparer.OrdinalIgnoreCase
      );

      for(var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
      {
         foreach(Match match in pattern.Matches(lines[lineIndex]))
         {
            if(!TryExtractEntryName(
               lines,
               lineIndex,
               match,
               out var name
            ))
            {
               continue;
            }

            var countryLabel = GetCountryLabel(match.Value);
            var entry = $"{name} | {countryLabel}";

            if(seenEntries.Add(entry))
            {
               entries.Add(entry);
            }

            if(entries.Count >= maxEntries)
            {
               return entries;
            }
         }
      }

      return entries;
   }

   internal static string FormatFindMatchesForTool(
      IReadOnlyList<PageMatch> matches
   )
   {
      if(matches.Count == 0)
      {
         return "No matching text found.";
      }

      return string.Join(
         Environment.NewLine,
         matches.Select(match => match.Snippet)
      );
   }

   private static string GetPageSearchText(WebPageContent pageContent)
   {
      return string.IsNullOrWhiteSpace(pageContent.MainTextFull)
         ? pageContent.MainText
         : pageContent.MainTextFull;
   }

   private static bool IsPrimaryCountryFind(string find)
   {
      return GetPrimaryCountryTerms().Any(term =>
         BuildFindTermRegex([term]).IsMatch(find)
      );
   }

   private static IEnumerable<string> GetPrimaryCountryTerms()
   {
      return
      [
         PrimaryCountry.CountryName,
         PrimaryCountry.LocalDisplayName,
         PrimaryCountry.ThreeLetterCode,
         PrimaryCountry.TwoLetterCode
      ];
   }

   private static string GetCountryLabel(string value)
   {
      foreach(var term in GetPrimaryCountryTerms())
      {
         var match = BuildFindTermRegex([term]).Match(value);

         if(match.Success)
         {
            return match.Value.Trim();
         }
      }

      return value.Trim();
   }

   private static bool TryExtractEntryName(
      IReadOnlyList<string> lines,
      int lineIndex,
      Match countryMatch,
      out string name
   )
   {
      var line = lines[lineIndex];
      var before = line[..countryMatch.Index];
      var after = line[(countryMatch.Index + countryMatch.Length)..];

      if(TryNormalizeNameCandidate(
         GetCellBeforeMatch(before),
         allowSingleWord: true,
         out name
      ))
      {
         return true;
      }

      if(TryNormalizeNameCandidate(
         GetCellAfterMatch(after),
         allowSingleWord: false,
         out name
      ))
      {
         return true;
      }

      var adjacentOffsets = string.IsNullOrWhiteSpace(
         GetCellAfterMatch(after)
      )
         ? new[] { 1, -1 }
         : new[] { -1, 1 };

      foreach(var offset in adjacentOffsets)
      {
         var adjacentIndex = lineIndex + offset;

         if(adjacentIndex < 0 || adjacentIndex >= lines.Count)
         {
            continue;
         }

         if(TryNormalizeNameCandidate(
            lines[adjacentIndex],
            allowSingleWord: true,
            out name
         ))
         {
            return true;
         }
      }

      name = string.Empty;
      return false;
   }

   private static string GetCellBeforeMatch(string value)
   {
      var lastPipeIndex = value.LastIndexOf('|');

      if(lastPipeIndex >= 0)
      {
         value = value[(lastPipeIndex + 1)..];
      }

      return Regex.Replace(
         value,
         @"^\s*\d+\s+",
         string.Empty,
         RegexOptions.CultureInvariant
      );
   }

   private static string GetCellAfterMatch(string value)
   {
      var firstPipeIndex = value.IndexOf('|');

      if(firstPipeIndex >= 0)
      {
         value = value[..firstPipeIndex];
      }

      return value;
   }

   private static bool TryNormalizeNameCandidate(
      string value,
      bool allowSingleWord,
      out string name
   )
   {
      name = string.Empty;
      var normalized = NormalizeMatchingRow(value);

      if(normalized.Contains('|'))
      {
         normalized = normalized[..normalized.IndexOf('|')];
      }

      normalized = Regex.Replace(
         normalized,
         @"^\s*\d+\s+",
         string.Empty,
         RegexOptions.CultureInvariant
      ).Trim(' ', '|', '-', ':');
      normalized = Regex.Replace(
         normalized,
         @"\s+,",
         ",",
         RegexOptions.CultureInvariant
      );

      if(string.IsNullOrWhiteSpace(normalized) ||
         normalized.Any(char.IsDigit))
      {
         return false;
      }

      var tokens = normalized.Split(
         ' ',
         StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries
      );

      if(tokens.Length == 0 ||
         (!allowSingleWord && tokens.Length < 2) ||
         tokens.Any(token => token.Length == 0 || token.All(
            character => !char.IsLetter(character)
         )))
      {
         return false;
      }

      if(tokens.Length == 1 && tokens[0].All(char.IsUpper))
      {
         return false;
      }

      var commaIndex = normalized.IndexOf(',');

      if(commaIndex > 0)
      {
         var surname = normalized[..commaIndex].Trim();
         var givenName = normalized[(commaIndex + 1)..]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

         if(!string.IsNullOrWhiteSpace(givenName))
         {
            normalized = $"{surname}, {givenName}";
         }
      }

      name = normalized;
      return true;
   }

   private static string NormalizeMatchingRow(string row)
   {
      var normalized = row
         .ReplaceLineEndings(" ")
         .Trim();
      normalized = WebPageTextNormalization.NormalizeGluedTableCellText(
         normalized
      );
      normalized = Regex.Replace(
         normalized,
         @"\s+",
         " ",
         RegexOptions.CultureInvariant
      );

      return Regex.Replace(
         normalized,
         @"\b(?<value>[^|]+?)\s+\|\s+\k<value>\b",
         "${value}",
         RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
      ).Trim();
   }

   private static void AddSnippetMatch(
      ICollection<PageMatch> matches,
      ISet<string> seenSnippets,
      string snippet,
      string find
   )
   {
      if(string.IsNullOrWhiteSpace(snippet) ||
         snippet.IndexOf(find, StringComparison.OrdinalIgnoreCase) < 0)
      {
         return;
      }

      var normalizedSnippet = snippet.Trim();

      if(!seenSnippets.Add(normalizedSnippet))
      {
         return;
      }

      matches.Add(new PageMatch("text", normalizedSnippet));
   }

   private static IEnumerable<string> ExtractTextSnippets(
      string text,
      string find,
      int contextLength = 60,
      int maxMatches = WebPageFetchDefaults.MaxFindInPageSnippetCount
   )
   {
      if(string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(find))
      {
         yield break;
      }

      var searchIndex = 0;
      var matches = 0;

      while(matches < maxMatches)
      {
         var index = text.IndexOf(
            find,
            searchIndex,
            StringComparison.OrdinalIgnoreCase
         );

         if(index < 0)
         {
            yield break;
         }

         var start = Math.Max(0, index - contextLength);
         var end = Math.Min(text.Length, index + find.Length + contextLength);
         var snippet = text[start..end].ReplaceLineEndings(" ").Trim();

         if(start > 0)
         {
            snippet = "..." + snippet;
         }

         if(end < text.Length)
         {
            snippet += "...";
         }

         yield return snippet;

         searchIndex = end;
         matches++;
      }
   }

   private static Regex BuildFindTermRegex(
      IReadOnlyCollection<string> terms
   )
   {
      var alternatives = terms
         .Select(BuildFindTermPattern)
         .ToArray();

      return new Regex(
         string.Join("|", alternatives),
         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
      );
   }

   private static string BuildFindTermPattern(string term)
   {
      var escapedTerm = Regex.Escape(term);

      return IsCountryCodeTerm(term)
         ? $@"(?<![\p{{L}}\p{{N}}]){escapedTerm}(?![\p{{L}}\p{{N}}])"
         : escapedTerm;
   }

   private static bool IsCountryCodeTerm(string term)
   {
      return term.Length is 2 or 3 &&
         term.All(character =>
            character is >= 'A' and <= 'Z' or >= 'a' and <= 'z'
         );
   }

   internal sealed record PageMatch(
      string Section,
      string Snippet
   );
}
