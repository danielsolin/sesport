using SESport.AI.WebPages;
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
      WebPageFetchErrorKind? fetchErrorKind = null
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

      if(relevantLinks is not null && relevantLinks.Count > 0)
      {
         builder.AppendLine("Relevant links:");

         foreach(var link in relevantLinks)
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

   internal static IReadOnlyList<string> ExtractMatchingRows(
      string text,
      string find,
      int maxRows = 50
   )
   {
      return ExtractMatchingRows(text, [find], maxRows);
   }

   internal static IReadOnlyList<string> ExtractMatchingRows(
      string text,
      IReadOnlyCollection<string> findTerms,
      int maxRows = 50
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

      foreach(var line in normalizedText.Split(
         '\n',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      ))
      {
         foreach(var candidate in ExtractMatchingRowCandidates(
            line,
            terms
         ))
         {
            var row = NormalizeMatchingRow(candidate);

            if(!ContainsAnyTerm(row, terms) ||
               !seenRows.Add(row))
            {
               continue;
            }

            rows.Add(row);

            if(rows.Count >= maxRows)
            {
               return rows;
            }
         }
      }

      return rows;
   }

   private static string FormatPageTextForToolResult(string text)
   {
      return text
         .Replace(" | ", " |" + Environment.NewLine, StringComparison.Ordinal)
         .Trim();
   }

   private static string GetPageSearchText(WebPageContent pageContent)
   {
      if(!string.IsNullOrWhiteSpace(pageContent.MainTextFull))
      {
         return pageContent.MainTextFull;
      }

      return pageContent.MainText;
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
      int maxMatches = 20
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
         yield return ExtractTextAroundMatch(line, matches[0].Index, 240);
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
      return term.Length == 3 &&
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

      normalized = WebPageContentFetchSupport.NormalizeGluedTableCellText(
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
         _ => "Fetch error"
      };
   }

   internal sealed record PageMatch(
      string Section,
      string Snippet
   );
}
