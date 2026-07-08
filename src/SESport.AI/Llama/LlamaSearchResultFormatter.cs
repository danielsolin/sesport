using System.Text.Json;

using SESport.AI.WebSearch;

namespace SESport.AI.Llama;

internal static class LlamaSearchResultFormatter
{
   public static string FormatSearchResults(
      IReadOnlyList<WebSearchResult> searchResults,
      JsonSerializerOptions jsonOptions
   )
   {
      if(searchResults.Count == 0)
      {
         return "[]";
      }

      var output = searchResults
         .Select(searchResult =>
         {
            return new
            {
               title = searchResult.Title,
               url = searchResult.Url,
               snippet = searchResult.Snippet,
               published_at = searchResult.PublishedAt?.ToString("O")
            };
         })
         .ToArray();

      return JsonSerializer.Serialize(output, jsonOptions);
   }

   public static string? GetSearchEngine(string? searchProviderDetails)
   {
      if(string.IsNullOrWhiteSpace(searchProviderDetails))
      {
         return null;
      }

      const string prefix = "engines=";

      return searchProviderDetails.StartsWith(
         prefix,
         StringComparison.OrdinalIgnoreCase
      )
         ? searchProviderDetails[prefix.Length..]
         : null;
   }
}
