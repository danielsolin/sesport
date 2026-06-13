using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SESport.AI.Providers;

public sealed class DuckDuckGoWebSearchClient : IWebSearchClient
{
   private static readonly Regex AnchorRegex = new(
      @"<a\b(?<attrs>[^>]*)>(?<text>.*?)</a>",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
   );

   private static readonly Regex SnippetRegex = new(
      @"class=""result__snippet""[^>]*>(?<text>.*?)</",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
   );

   private readonly HttpClient httpClient;
   private readonly ILogger<DuckDuckGoWebSearchClient> logger;

   public DuckDuckGoWebSearchClient(
      HttpClient httpClient,
      ILogger<DuckDuckGoWebSearchClient> logger
   )
   {
      this.httpClient = httpClient;
      this.logger = logger;
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
      if(logger.IsEnabled(LogLevel.Debug))
      {
         logger.LogDebug(
            "DuckDuckGo search start url={Url} query={Query} " +
            "max_results={MaxResults}",
            url,
            TruncateForLog(query, 240),
            maxResults
         );
      }

      using var request = new HttpRequestMessage(HttpMethod.Get, url);
      using var response = await httpClient.SendAsync(
         request,
         cancellationToken
      );
      var html = await response.Content.ReadAsStringAsync(cancellationToken);

      if(!response.IsSuccessStatusCode)
      {
         logger.LogWarning(
            "DuckDuckGo search failed url={Url} status={StatusCode} " +
            "query={Query} body={Body}",
            url,
            (int)response.StatusCode,
            TruncateForLog(query, 240),
            TruncateForLog(html, 1200)
         );

         response.EnsureSuccessStatusCode();
      }

      var results = ParseResults(html)
         .Take(Math.Clamp(maxResults, 1, 10))
         .ToList();

      if(results.Count == 0)
      {
         logger.LogWarning(
            "DuckDuckGo search returned no results url={Url} " +
            "query={Query} html_length={HtmlLength} html_head={HtmlHead}",
            url,
            TruncateForLog(query, 240),
            html.Length,
            TruncateForLog(html, 2000)
         );
      }
      else if(logger.IsEnabled(LogLevel.Debug))
      {
         logger.LogDebug(
            "DuckDuckGo search returned results={ResultCount} " +
            "first_result={FirstResult}",
            results.Count,
            $"{results[0].Title} | {results[0].Url}"
         );
      }

      return results;
   }

   private static string TruncateForLog(string value, int maxLength)
   {
      if(value.Length <= maxLength)
      {
         return value;
      }

      return value[..maxLength] + "...";
   }

   private static IEnumerable<WebSearchResult> ParseResults(string html)
   {
      foreach(Match anchorMatch in AnchorRegex.Matches(html))
      {
         var attrs = anchorMatch.Groups["attrs"].Value;
         var href = ExtractAttribute(attrs, "href");

         if(string.IsNullOrWhiteSpace(href) ||
            !IsResultAnchor(attrs, href))
         {
            continue;
         }

         var title = CleanHtml(anchorMatch.Groups["text"].Value);
         var url = NormalizeUrl(href);
         var snippet = string.Empty;

         var snippetWindow = html.Substring(
            anchorMatch.Index,
            Math.Min(1000, html.Length - anchorMatch.Index)
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

   private static bool IsResultAnchor(string attrs, string href)
   {
      if(href.Contains("uddg=", StringComparison.OrdinalIgnoreCase))
      {
         return true;
      }

      var className = ExtractAttribute(attrs, "class");

      return className.Contains("result__a", StringComparison.OrdinalIgnoreCase) ||
         className.Contains("result-link", StringComparison.OrdinalIgnoreCase) ||
         className.Contains("result__title-link", StringComparison.OrdinalIgnoreCase);
   }

   private static string ExtractAttribute(
      string attrs,
      string attributeName
   )
   {
      var match = Regex.Match(
         attrs,
         $@"\b{Regex.Escape(attributeName)}=""(?<value>[^""]*)""",
         RegexOptions.IgnoreCase
      );

      return match.Success ? match.Groups["value"].Value : "";
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
