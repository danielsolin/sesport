using System.Text.RegularExpressions;

namespace SESport.AI.WebSearch;

internal static partial class SearchResultRelevanceGuard
{
   private const int MinimumQueryTermCount = 4;
   private const int MinimumResultCount = 5;
   private const int MinimumMatchingTermCount = 2;

   internal static bool IsCatastrophicallyIrrelevant(
      string query,
      IReadOnlyList<WebSearchResult> results
   )
   {
      if(results.Count < MinimumResultCount)
      {
         return false;
      }

      var queryTerms = ReadDistinctTerms(query);
      if(queryTerms.Count < MinimumQueryTermCount)
      {
         return false;
      }

      return !results.Any(result => CountMatchingTerms(
         queryTerms,
         result.Title + " " + result.Snippet
      ) >= MinimumMatchingTermCount);
   }

   private static HashSet<string> ReadDistinctTerms(string text)
   {
      return WordPattern()
         .Matches(text)
         .Select(match => match.Value.ToLowerInvariant())
         .Where(term => term.Length >= 3)
         .Where(term => !term.All(char.IsDigit))
         .ToHashSet(StringComparer.Ordinal);
   }

   private static int CountMatchingTerms(
      HashSet<string> queryTerms,
      string text
   )
   {
      return ReadDistinctTerms(text).Count(queryTerms.Contains);
   }

   [GeneratedRegex(@"[\p{L}\p{N}]+")]
   private static partial Regex WordPattern();
}
