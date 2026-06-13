using System.Net;
using System.Text.RegularExpressions;

namespace SESport.AI.Providers;

public sealed class DuckDuckGoWebSearchClient : IWebSearchClient
{
   private static readonly Regex LinkRegex = new(
      @"<a[^>]*class=""result__a""[^>]*href=""(?<href>[^""]+)""[^>]*>" +
      @"(?<text>.*?)</a>",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
   );

   private static readonly Regex SnippetRegex = new(
      @"class=""result__snippet""[^>]*>(?<text>.*?)</",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
   );

   private readonly HttpClient httpClient;

   public DuckDuckGoWebSearchClient(HttpClient httpClient)
   {
      this.httpClient = httpClient;
   }

   public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(query))
      {
         return [];
      }

      var url =
         "https://html.duckduckgo.com/html/?q=" +
         Uri.EscapeDataString(query) +
         "&kl=se-sv&kp=-1";
      var html = await httpClient.GetStringAsync(
         url,
         cancellationToken
      );
      var results = ParseResults(html)
         .Take(Math.Clamp(maxResults, 1, 10))
         .ToList();

      return results;
   }

   private static IEnumerable<WebSearchResult> ParseResults(string html)
   {
      foreach(Match linkMatch in LinkRegex.Matches(html))
      {
         var title = CleanHtml(linkMatch.Groups["text"].Value);
         var url = NormalizeUrl(linkMatch.Groups["href"].Value);
         var snippet = string.Empty;

         var snippetWindow = html.Substring(
            linkMatch.Index,
            Math.Min(1000, html.Length - linkMatch.Index)
         );
         var snippetMatch = SnippetRegex.Match(snippetWindow);

         if(snippetMatch.Success)
         {
            snippet = CleanHtml(snippetMatch.Groups["text"].Value);
         }

         if(string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
         {
            continue;
         }

         yield return new WebSearchResult(
            title,
            url,
            string.IsNullOrWhiteSpace(snippet) ? null : snippet
         );
      }
   }

   private static string NormalizeUrl(string href)
   {
      var normalized = WebUtility.HtmlDecode(href).Trim();

      if(normalized.StartsWith("//", StringComparison.Ordinal))
      {
         normalized = "https:" + normalized;
      }

      try
      {
         var uri = new Uri(normalized);
         var query = uri.Query;
         var match = Regex.Match(
            query,
            @"(?:\?|&)uddg=(?<url>[^&]+)",
            RegexOptions.IgnoreCase
         );

         if(match.Success)
         {
            return Uri.UnescapeDataString(match.Groups["url"].Value);
         }
      }
      catch(UriFormatException)
      {
      }

      return normalized;
   }

   private static string CleanHtml(string value)
   {
      var text = WebUtility.HtmlDecode(value);
      text = Regex.Replace(text, "<[^>]+>", "");
      return Regex.Replace(text, @"\s+", " ").Trim();
   }
}
