using SESport.AI.WebPages;
using SESport.Core.Formatting;

using System.Text;
using System.Text.RegularExpressions;

namespace SESport.AI.Llama;

internal static class LlamaPageToolFormatter
{
   internal static string FormatPageContentText(
      string referenceLabel,
      string referenceValue,
      string title,
      string url,
      string? searchSnippet,
      DateTimeOffset? publishedAt,
      IReadOnlyList<string>? headings,
      IReadOnlyList<WebPageRelevantLink>? relevantLinks,
      string? highlightedRowLabel,
      IReadOnlyList<string>? highlightedRows,
      string? mainText,
      string? fetchErrorMessage = null,
      WebPageFetchErrorKind? fetchErrorKind = null,
      string? renderWarning = null
   )
   {
      var builder = new StringBuilder();

      builder.AppendLine($"{referenceLabel}: {referenceValue}");
      builder.AppendLine($"Title: {title}");
      builder.AppendLine($"URL: {url}");

      if(publishedAt is not null)
      {
         builder.AppendLine($"Published: {publishedAt:O}");
      }

      if(!string.IsNullOrWhiteSpace(searchSnippet))
      {
         builder.AppendLine("Search snippet:");
         builder.AppendLine(searchSnippet.Trim());
      }

      if(headings is not null && headings.Count > 0)
      {
         builder.AppendLine("Headings:");

         foreach(var heading in headings)
         {
            builder.AppendLine($"- {heading}");
         }
      }

      if(!string.IsNullOrWhiteSpace(renderWarning))
      {
         builder.AppendLine("Render warning:");
         builder.AppendLine(renderWarning.Trim());
      }

      var pdfLinks = relevantLinks?
         .Where(link => IsPdfUrl(link.Url))
         .ToArray() ?? [];

      if(pdfLinks.Length > 0)
      {
         builder.AppendLine("PDF links:");

         foreach(var link in pdfLinks)
         {
            builder.AppendLine($"- {link.Label}: {link.Url}");
         }
      }

      if(!string.IsNullOrWhiteSpace(highlightedRowLabel) &&
         highlightedRows is not null &&
         highlightedRows.Count > 0)
      {
         builder.AppendLine($"{highlightedRowLabel}:");
         builder.AppendLine($"Count: {highlightedRows.Count}");

         foreach(var row in highlightedRows)
         {
            builder.AppendLine($"- {row}");
         }
      }

      if(!string.IsNullOrWhiteSpace(mainText))
      {
         builder.AppendLine("Page text:");
         builder.AppendLine(FormatPageTextForToolResult(mainText));
      }
      else if(!string.IsNullOrWhiteSpace(fetchErrorMessage))
      {
         builder.AppendLine("Fetch error:");
         if(fetchErrorKind is not null)
         {
            builder.AppendLine(
               $"{DescribeFetchErrorKind(fetchErrorKind)}: " +
               fetchErrorMessage.Trim()
            );
         }
         else
         {
            builder.AppendLine(fetchErrorMessage.Trim());
         }
      }
      else if(headings is null || headings.Count == 0)
      {
         builder.AppendLine("Page text: (empty)");
      }

      return builder.ToString().Trim();
   }

   private static bool IsPdfUrl(string url)
   {
      if(!Uri.TryCreate(url, UriKind.Absolute, out var uri))
      {
         return false;
      }

      return uri.AbsolutePath.EndsWith(
         ".pdf",
         StringComparison.OrdinalIgnoreCase
      );
   }

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
         AddSnippetMatch(matches, seenSnippets, "text", snippet, find);
      }

      return matches;
   }

   internal static IReadOnlyList<string> ExtractMatchingCountryEntries(
      string text,
      string find,
      int maxEntries = LlamaServerDefaults.MaxFindInPageSnippetCount
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
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      );
      var countryFindTerm = GetPrimaryCountryTerms()
         .FirstOrDefault(term =>
            BuildFindTermRegex([term]).IsMatch(find)
         );
      var pattern = BuildFindTermRegex(
         [countryFindTerm ?? find]
      );
      var entries = new List<string>();
      var seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

   internal static IReadOnlyList<string> ExtractMatchingRows(
      string text,
      string find,
      int maxRows = LlamaServerDefaults.MaxFindInPageSnippetCount
   )
   {
      return ExtractMatchingRows(text, [find], maxRows);
   }

   internal static IReadOnlyList<string> ExtractMatchingRows(
      string text,
      IReadOnlyCollection<string> findTerms,
      int maxRows = LlamaServerDefaults.MaxFindInPageSnippetCount
   )
   {
      if(string.IsNullOrWhiteSpace(text) ||
         findTerms.Count == 0)
      {
         return [];
      }

      var terms = findTerms
         .Where(term => !string.IsNullOrWhiteSpace(term))
         .Select(term => term.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray();

      if(terms.Length == 0)
      {
         return [];
      }

      var rows = new List<string>();
      var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      var normalizedText = text.ReplaceLineEndings("\n");
      var lines = normalizedText.Split(
         '\n',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      );
      var flattenedText = string.Join(" ", lines);

      foreach(var candidate in ExtractPipeDelimitedRows(
         flattenedText,
         terms
      ))
      {
         AddMatchingRow(rows, seenRows, candidate, terms, maxRows);

         if(rows.Count >= maxRows)
         {
            return rows;
         }
      }

      if(rows.Count > 0)
      {
         return rows;
      }

      foreach(var line in lines)
      {
         foreach(var candidate in ExtractMatchingRowCandidates(
            line,
            terms
         ))
         {
            AddMatchingRow(rows, seenRows, candidate, terms, maxRows);

            if(rows.Count >= maxRows)
            {
               return rows;
            }
         }
      }

      return rows;
   }

   internal static bool AreRowsLikelyPartial(
      IReadOnlyList<string> rows
   )
   {
      if(rows.Count < 2)
      {
         return false;
      }

      var rowShapes = rows
         .Select(GetRowShape)
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray();

      return rowShapes.Length == 1;
   }

   private static string GetRowShape(string row)
   {
      return Regex.Replace(
         row,
         @"\|\s*\d+(?:[.,]\d+)?[a-z]?\b",
         "| #",
         RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
      ).Trim();
   }

   private static IEnumerable<string> ExtractPipeDelimitedRows(
      string text,
      IReadOnlyCollection<string> terms
   )
   {
      foreach(Match match in BuildFindTermRegex(terms).Matches(text))
      {
         if(TryExtractPipeDelimitedRow(
            text,
            match.Index,
            out var row
         ))
         {
            yield return row;
         }
      }
   }

   private static void AddMatchingRow(
      ICollection<string> rows,
      ISet<string> seenRows,
      string candidate,
      IReadOnlyCollection<string> terms,
      int maxRows
   )
   {
      if(rows.Count >= maxRows)
      {
         return;
      }

      var row = NormalizeMatchingRow(candidate);

      if(!ContainsAnyTerm(row, terms) || !seenRows.Add(row))
      {
         return;
      }

      rows.Add(row);
   }

   private static string FormatPageTextForToolResult(string text)
   {
      return text.Trim();
   }

   private static string GetPageSearchText(WebPageContent pageContent)
   {
      if(!string.IsNullOrWhiteSpace(pageContent.MainTextFull))
      {
         return pageContent.MainTextFull;
      }

      return pageContent.MainText;
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
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
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

   private static void AddSnippetMatch(
      ICollection<PageMatch> matches,
      ISet<string> seenSnippets,
      string section,
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

      matches.Add(new PageMatch(section, normalizedSnippet));
   }

   private static IEnumerable<string> ExtractTextSnippets(
      string text,
      string find,
      int contextLength = 60,
      int maxMatches = LlamaServerDefaults.MaxFindInPageSnippetCount
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

   private static IEnumerable<string> ExtractMatchingRowCandidates(
      string line,
      IReadOnlyCollection<string> terms
   )
   {
      var matches = BuildFindTermRegex(terms).Matches(line);

      if(matches.Count == 0)
      {
         yield break;
      }

      var hasPipeDelimitedRows = false;

      foreach(Match match in matches)
      {
         if(TryExtractPipeDelimitedRow(
            line,
            match.Index,
            out var pipeDelimitedRow
         ))
         {
            hasPipeDelimitedRows = true;
            yield return pipeDelimitedRow;
         }
      }

      if(hasPipeDelimitedRows)
      {
         yield break;
      }

      if(line.Length <= 600)
      {
         yield return line;
         yield break;
      }

      foreach(var segment in ExtractRepeatedFindSegments(line, terms))
      {
         yield return segment;
      }
   }

   private static IEnumerable<string> ExtractRepeatedFindSegments(
      string line,
      IReadOnlyCollection<string> terms
   )
   {
      var pattern = BuildFindTermRegex(terms);
      var matches = pattern.Matches(line);

      if(matches.Count <= 1)
      {
         yield return ExtractTextAroundMatch(
            line,
            matches[0].Index,
            LlamaServerDefaults.PreviewSnippetCharacters
         );
         yield break;
      }

      for(var index = 0; index < matches.Count; index++)
      {
         var start = matches[index].Index;
         var end = index + 1 < matches.Count
            ? matches[index + 1].Index
            : Math.Min(line.Length, start + 600);

         yield return line[start..end];
      }
   }

   private static string ExtractTextAroundMatch(
      string text,
      int matchIndex,
      int maxCharacters
   )
   {
      var start = Math.Max(0, matchIndex - maxCharacters / 2);
      var end = Math.Min(text.Length, start + maxCharacters);

      return text[start..end];
   }

   private static bool TryExtractPipeDelimitedRow(
      string text,
      int matchIndex,
      out string row
   )
   {
      row = string.Empty;
      var remainingText = text[matchIndex..];
      var match = Regex.Match(
         remainingText,
         @"^(?<row>.{0,260}?\|\s*\d+(?:[.,]\d+)?[a-z]?\b)",
         RegexOptions.CultureInvariant | RegexOptions.Singleline
      );

      if(!match.Success)
      {
         return false;
      }

      var candidate = match.Groups["row"].Value;

      if(candidate.Count(character => character == '|') < 2)
      {
         return false;
      }

      row = candidate;
      return true;
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

   private static bool ContainsAnyTerm(
      string value,
      IReadOnlyCollection<string> terms
   )
   {
      return BuildFindTermRegex(terms).IsMatch(value);
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

      normalized = Regex.Replace(
         normalized,
         @"\b(?<value>[^|]+?)\s+\|\s+\k<value>\b",
         "${value}",
         RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
      );

      return normalized.Trim();
   }

   private static string DescribeFetchErrorKind(
      WebPageFetchErrorKind? fetchErrorKind
   )
   {
      return fetchErrorKind switch
      {
         WebPageFetchErrorKind.BrowserBlocked => "Browser blocked",
         WebPageFetchErrorKind.Timeout => "Timeout",
         WebPageFetchErrorKind.HttpError => "HTTP error",
         _ => "Fetch error"
      };
   }

   internal sealed record PageMatch(
      string Section,
      string Snippet
   );
}
