using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SESport.AI.Providers;

public sealed class WebPageContentClient : IWebPageContentClient
{
   // Keep extracted page text small enough for tool traces and follow-up calls.
   private const int MaxMainTextLength = 8000;

   private static readonly Regex TitleRegex = new(
      @"<title\b[^>]*>(?<text>.*?)</title>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex ArticleRegex = new(
      @"<article\b[^>]*>(?<html>.*?)</article>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex MainRegex = new(
      @"<main\b[^>]*>(?<html>.*?)</main>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex BodyRegex = new(
      @"<body\b[^>]*>(?<html>.*?)</body>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex StripBlockRegex = new(
      @"<(script|style|noscript|iframe|svg|footer|header|nav|aside|form|button|input|select|option|textarea|canvas|video|audio)\b[^>]*>.*?</\1>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex BlockTagRegex = new(
      @"</?\s*(p|div|section|article|main|li|tr|td|th|table|ul|ol|blockquote|pre|h[1-6]|figcaption|figure|header|footer|nav|aside|dl|dt|dd|time|br|hr)\b[^>]*>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex CommentRegex = new(
      @"<!--.*?-->",
      RegexOptions.Singleline | RegexOptions.CultureInvariant
   );

   private static readonly Regex WhitespaceRegex = new(
      @"[ \t\f\v]+",
      RegexOptions.CultureInvariant
   );

   private readonly HttpClient httpClient;

   public WebPageContentClient(HttpClient httpClient)
   {
      this.httpClient = httpClient;
   }

   public async Task<WebPageContent?> FetchAsync(
      string url,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(url) ||
         !Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
      {
         return null;
      }

      using var request = new HttpRequestMessage(HttpMethod.Get, absoluteUrl);
      request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");

      using var response = await httpClient.SendAsync(
         request,
         cancellationToken
      );
      var rawHtml = await response.Content.ReadAsStringAsync(
         cancellationToken
      );

      if(!response.IsSuccessStatusCode ||
         string.IsNullOrWhiteSpace(rawHtml))
      {
         return null;
      }

      return ExtractPageContent(absoluteUrl.ToString(), rawHtml);
   }

   private static WebPageContent? ExtractPageContent(
      string url,
      string rawHtml
   )
   {
      var title = ExtractTitle(rawHtml);
      var publishedAt = ExtractPublishedAt(rawHtml);
      var mainText = ExtractMainText(rawHtml);

      if(string.IsNullOrWhiteSpace(title) &&
         string.IsNullOrWhiteSpace(mainText))
      {
         return null;
      }

      return new WebPageContent(
         string.IsNullOrWhiteSpace(title) ? url : title,
         url,
         publishedAt,
         mainText
      );
   }

   private static string ExtractTitle(string rawHtml)
   {
      var ogTitle = ExtractMetaContent(
         rawHtml,
         "property",
         "og:title"
      );

      if(!string.IsNullOrWhiteSpace(ogTitle))
      {
         return ogTitle;
      }

      var match = TitleRegex.Match(rawHtml);
      return match.Success ? CleanText(match.Groups["text"].Value) : "";
   }

   private static DateTimeOffset? ExtractPublishedAt(string rawHtml)
   {
      var candidates = new[]
      {
         ExtractMetaContent(rawHtml, "property", "article:published_time"),
         ExtractMetaContent(rawHtml, "property", "article:modified_time"),
         ExtractMetaContent(rawHtml, "name", "date"),
         ExtractMetaContent(rawHtml, "name", "pubdate"),
         ExtractMetaContent(rawHtml, "itemprop", "datePublished"),
         ExtractMetaContent(rawHtml, "itemprop", "dateModified"),
         ExtractTimeAttribute(rawHtml)
      };

      foreach(var candidate in candidates)
      {
         if(TryParseDate(candidate, out var publishedAt))
         {
            return publishedAt;
         }
      }

      return null;
   }

   private static string ExtractMainText(string rawHtml)
   {
      var candidate = ExtractSection(rawHtml, ArticleRegex)
         ?? ExtractSection(rawHtml, MainRegex)
         ?? ExtractSection(rawHtml, BodyRegex)
         ?? rawHtml;

      candidate = StripBlockRegex.Replace(candidate, " ");
      candidate = CommentRegex.Replace(candidate, " ");
      candidate = BlockTagRegex.Replace(candidate, "\n");
      candidate = Regex.Replace(
         candidate,
         @"<[^>]+>",
         " ",
         RegexOptions.CultureInvariant
      );
      candidate = WebUtility.HtmlDecode(candidate);
      candidate = candidate.Replace("\r", "\n");
      candidate = WhitespaceRegex.Replace(candidate, " ");
      candidate = Regex.Replace(
         candidate,
         @"\n[ \t]+",
         "\n",
         RegexOptions.CultureInvariant
      );
      candidate = Regex.Replace(
         candidate,
         @"\n{3,}",
         "\n\n",
         RegexOptions.CultureInvariant
      );

      var lines = candidate
         .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
         .Where(line => !string.IsNullOrWhiteSpace(line))
         .ToArray();

      var text = string.Join(Environment.NewLine, lines);

      if(text.Length > MaxMainTextLength)
      {
         return text[..MaxMainTextLength].TrimEnd() + "...";
      }

      return text.Trim();
   }

   private static string? ExtractSection(
      string rawHtml,
      Regex sectionRegex
   )
   {
      var match = sectionRegex.Match(rawHtml);
      return match.Success ? match.Groups["html"].Value : null;
   }

   private static string ExtractMetaContent(
      string rawHtml,
      string attributeName,
      string attributeValue
   )
   {
      var pattern =
         $"<meta\\b[^>]*{attributeName}\\s*=\\s*[\"']" +
         Regex.Escape(attributeValue) +
         "[\"'][^>]*content\\s*=\\s*[\"'](?<content>[^\"']+)[\"'][^>]*>";

      var match = Regex.Match(
         rawHtml,
         pattern,
         RegexOptions.IgnoreCase |
         RegexOptions.Singleline |
         RegexOptions.CultureInvariant
      );

      if(match.Success)
      {
         return WebUtility.HtmlDecode(match.Groups["content"].Value);
      }

      pattern =
         $"<meta\\b[^>]*content\\s*=\\s*[\"'](?<content>[^\"']+)[\"'][^>]*" +
         $"{attributeName}\\s*=\\s*[\"']" +
         Regex.Escape(attributeValue) +
         "[\"'][^>]*>";

      match = Regex.Match(
         rawHtml,
         pattern,
         RegexOptions.IgnoreCase |
         RegexOptions.Singleline |
         RegexOptions.CultureInvariant
      );

      return match.Success
         ? WebUtility.HtmlDecode(match.Groups["content"].Value)
         : "";
   }

   private static string ExtractTimeAttribute(string rawHtml)
   {
      var match = Regex.Match(
         rawHtml,
         @"<time\b[^>]*datetime\s*=\s*[""'](?<content>[^""']+)[""'][^>]*>",
         RegexOptions.IgnoreCase |
         RegexOptions.Singleline |
         RegexOptions.CultureInvariant
      );

      return match.Success
         ? WebUtility.HtmlDecode(match.Groups["content"].Value)
         : "";
   }

   private static bool TryParseDate(
      string? candidate,
      out DateTimeOffset publishedAt
   )
   {
      publishedAt = default;

      if(string.IsNullOrWhiteSpace(candidate))
      {
         return false;
      }

      return DateTimeOffset.TryParse(
         candidate,
         out publishedAt
      );
   }

   private static string CleanText(string value)
   {
      return WebUtility.HtmlDecode(value)
         .ReplaceLineEndings(" ")
         .Trim();
   }
}
