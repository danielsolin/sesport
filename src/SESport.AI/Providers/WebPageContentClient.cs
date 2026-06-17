using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace SESport.AI.Providers;

public sealed class WebPageContentClient : IWebPageContentClient
{
   // Single knob for the maximum returned text size.
   private const int MaxReturnedTextLength = 8000;
   private const string CutOffMarker = "[CUTOFF]";

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
      request.Headers.TryAddWithoutValidation(
         "User-Agent",
         "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
         "(KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36"
      );
      request.Headers.TryAddWithoutValidation(
         "Accept-Language",
         "en-US,en;q=0.9"
      );

      using var response = await httpClient.SendAsync(
         request,
         cancellationToken
      );
      if(IsPdfResponse(response, absoluteUrl))
      {
         return null;
      }

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

   private static readonly Regex TableRegex = new(
      @"<table\b[^>]*>(?<html>.*?)</table>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex HeadingRegex = new(
      @"<h[1-6]\b[^>]*>(?<text>.*?)</h[1-6]>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex JsonLdScriptRegex = new(
      @"<script\b[^>]*type\s*=\s*[\""']application/ld\+json[\""'][^>]*>" +
      @"(?<json>.*?)</script>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex ScriptRegex = new(
      @"<script\b[^>]*>(?<script>.*?)</script>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex NextFlightPushRegex = new(
      @"self\.__next_f\.push\(\[1," +
      @"""(?<payload>(?:\\.|[^""\\])*)""\]\)",
      RegexOptions.IgnoreCase |
      RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex StructuredEntityRegex = new(
      "\"type\":\"(?<type>athlete|player|team)\"(?<block>.{0,1500})",
      RegexOptions.IgnoreCase |
      RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex NoisyHeadingTagRegex = new(
      @"<(form|select|option|input|button|textarea|label|fieldset|legend)\b",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
   );

   private static readonly Regex CountryElementRegex = new(
      @"<[^>]*(class|alt|title|aria-label|data-country|data-country-code|" +
      @"data-country-name|data-iso)\s*=\s*[""'][^""']+[""'][^>]*>",
      RegexOptions.IgnoreCase |
      RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Lazy<IReadOnlyDictionary<string, string>>
      CountryNameLookup = new(BuildCountryNameLookup);

   private static readonly Regex BodyRegex = new(
      @"<body\b[^>]*>(?<html>.*?)</body>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex StripBlockRegex = new(
      @"<(script|style|noscript|iframe|svg|footer|header|nav|aside|" +
      @"form|button|input|select|option|textarea|canvas|video|audio)\b" +
      @"[^>]*>.*?</\1>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline |
      RegexOptions.CultureInvariant
   );

   private static readonly Regex BlockTagRegex = new(
      @"</?\s*(p|div|section|article|main|li|tr|td|th|table|ul|ol|" +
      @"blockquote|pre|h[1-6]|figcaption|figure|header|footer|nav|" +
      @"aside|dl|dt|dd|time|br|hr)\b[^>]*>",
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

   private static readonly Regex CssNoiseLineRegex = new(
      @"^(?:\d+(?:\.\d+)?(?:px|em|rem|vh|vw|%)\s*){2,}$",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
   );

   private static readonly Regex LayoutNoiseTokenRegex = new(
      @"^(?:\d+(?:\.\d+)?(?:px|em|rem|vh|vw|%)|" +
      @"[a-z]{1,4}\d{0,3}|\d{1,4})$",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
   );

   private static readonly Regex PxNoiseTokenRegex = new(
      @"^\d+(?:\.\d+)?px$",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
   );

   private readonly HttpClient httpClient;

   private static bool IsPdfResponse(
      HttpResponseMessage response,
      Uri absoluteUrl
   )
   {
      var contentType = response.Content.Headers.ContentType?.MediaType;

      if(string.Equals(
         contentType,
         "application/pdf",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return true;
      }

      if(absoluteUrl.AbsolutePath.EndsWith(
         ".pdf",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return true;
      }

      return string.Equals(
         contentType,
         "application/x-pdf",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static WebPageContent? ExtractPageContent(
      string url,
      string rawHtml
   )
   {
      var title = ExtractTitle(rawHtml);
      var publishedAt = ExtractPublishedAt(rawHtml);
      var headings = ExtractHeadings(rawHtml);
      var mainTextInfo = ExtractMainText(rawHtml);

      if(string.IsNullOrWhiteSpace(title) &&
         string.IsNullOrWhiteSpace(mainTextInfo.Text) &&
         headings.Count == 0)
      {
         return null;
      }

      return new WebPageContent(
         string.IsNullOrWhiteSpace(title) ? url : title,
         url,
         publishedAt,
         headings,
         mainTextInfo.Text,
         mainTextInfo.HasBodyText,
         mainTextInfo.FullText
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

   private static MainTextResult ExtractMainText(string rawHtml)
   {
      var displayTextInfo = ExtractMainTextVariant(rawHtml, false);
      var searchTextInfo = ExtractSearchText(rawHtml);

      return new MainTextResult(
         displayTextInfo.Text,
         displayTextInfo.HasBodyText,
         searchTextInfo
      );
   }

   private static MainTextResult ExtractMainTextVariant(
      string rawHtml,
      bool expanded
   )
   {
      var candidate = ExtractContentCandidate(rawHtml);
      candidate = NormalizeCountryMarkers(candidate);
      var bodyText = ExtractPlainText(candidate);
      var tableText = ExtractTableText(candidate);
      var supplementalText = ExtractSupplementalText(rawHtml, expanded);

      var mainSections = new List<string>();
      AddSection(mainSections, bodyText);
      AddSection(mainSections, tableText);

      if(mainSections.Count > 0)
      {
         var text = string.Join(
            Environment.NewLine + Environment.NewLine,
            mainSections
         );

         if(ContainsLayoutNoise(bodyText) &&
            !string.IsNullOrWhiteSpace(supplementalText))
         {
            text = supplementalText.Trim();
         }

         var limitedText = LimitReturnedText(
            text,
            MaxReturnedTextLength
         );

         return new MainTextResult(
            limitedText,
            true,
            limitedText
         );
      }

      return new MainTextResult(
         supplementalText.Trim(),
         false,
         supplementalText.Trim()
      );
   }

   private static bool ContainsLayoutNoise(string text)
   {
      return text.Contains("0PX", StringComparison.OrdinalIgnoreCase) ||
         text.Contains(
            "SKIP TO MAIN CONTENT",
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static string ExtractPlainText(string html)
   {
      html = StripBlockRegex.Replace(html, " ");
      html = CommentRegex.Replace(html, " ");
      html = BlockTagRegex.Replace(html, "\n");
      html = Regex.Replace(
         html,
         @"<[^>]+>",
         " ",
         RegexOptions.CultureInvariant
      );
      html = WebUtility.HtmlDecode(html);
      html = html.Replace("\r", "\n");
      html = WhitespaceRegex.Replace(html, " ");
      html = Regex.Replace(
         html,
         @"\n[ \t]+",
         "\n",
         RegexOptions.CultureInvariant
      );
      html = Regex.Replace(
         html,
         @"\n{3,}",
         "\n\n",
         RegexOptions.CultureInvariant
      );

      var lines = html
         .Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries
         )
         .Where(line => !string.IsNullOrWhiteSpace(line))
         .Where(line => !IsNoiseLine(line))
         .ToArray();

      return string.Join(Environment.NewLine, lines);
   }

   private static string ExtractTableText(string html)
   {
      var sections = new List<string>();

      foreach(Match match in TableRegex.Matches(html))
      {
         var tableHtml = match.Groups["html"].Value;
         var tableText = ExtractPlainText(tableHtml);

         if(!string.IsNullOrWhiteSpace(tableText))
         {
            sections.Add(tableText);
         }
      }

      return string.Join(
         Environment.NewLine + Environment.NewLine,
         sections
      );
   }

   private static bool IsNoiseLine(string line)
   {
      var trimmed = line.Trim();
      var tokens = trimmed
         .Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries
         )
         .Where(token => token.Length > 0)
         .ToArray();

      if(trimmed.Length == 0)
      {
         return true;
      }

      if(CssNoiseLineRegex.IsMatch(trimmed))
      {
         return true;
      }

      if(tokens.Length == 1 && LayoutNoiseTokenRegex.IsMatch(tokens[0]))
      {
         return true;
      }

      if(tokens.Any(LayoutNoiseTokenRegex.IsMatch) &&
         !trimmed.Any(char.IsLower))
      {
         return true;
      }

      if(tokens.Length > 0 && LayoutNoiseTokenRegex.IsMatch(tokens[0]))
      {
         return true;
      }

      if(trimmed.Length >= 12 &&
         Regex.IsMatch(
            trimmed,
            @"^(?:[0-9]+(?:\.[0-9]+)?[a-z%]+(?:\s+|$))+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
         ))
      {
         return true;
      }

      if(tokens.Length >= 3)
      {
         var noiseTokenCount = CountLayoutNoiseTokens(tokens);
         var letterCount = trimmed.Count(char.IsLetter);
         var digitCount = trimmed.Count(char.IsDigit);
         var uppercaseWordCount = tokens.Count(token =>
            token.Any(char.IsLetter) &&
            token.All(ch => !char.IsLetter(ch) || char.IsUpper(ch))
         );

         if(noiseTokenCount == tokens.Length)
         {
            return true;
         }

         if(noiseTokenCount >= tokens.Length - 1 &&
            letterCount <= 4)
         {
            return true;
         }

         if(noiseTokenCount >= 2 &&
            trimmed.Any(char.IsLetter) &&
            uppercaseWordCount >= tokens.Length - noiseTokenCount)
         {
            return true;
         }

         if(letterCount == 0 && digitCount > 0)
         {
            return true;
         }
      }

      if(trimmed.Length >= 16)
      {
         var letterCount = trimmed.Count(char.IsLetter);
         var tokenCount = tokens.Length;

         if(tokenCount >= 4 &&
            letterCount * 5 < trimmed.Length)
         {
            return true;
         }
      }

      return false;
   }

   private static int CountLayoutNoiseTokens(string[] tokens)
   {
      return tokens.Count(token =>
         (LayoutNoiseTokenRegex.IsMatch(token) ||
          PxNoiseTokenRegex.IsMatch(token)) &&
         token.Count(char.IsLetter) <= 4
      );
   }

   private static int ScoreText(string text)
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         return int.MinValue;
      }

      var score = 0;

      foreach(var line in text.Split(
         '\n',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      ))
      {
         if(string.IsNullOrWhiteSpace(line))
         {
            continue;
         }

         score += Math.Min(line.Length, 120);
         score += line.Count(char.IsLetter);

         if(IsNoiseLine(line))
         {
            score -= 250;
         }

         if(line.StartsWith(
            "Structured entities:",
            StringComparison.OrdinalIgnoreCase
         ))
         {
            score += 300;
         }

         if(line.StartsWith("athlete:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("player:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("team:", StringComparison.OrdinalIgnoreCase))
         {
            score += 120;
         }
      }

      return score;
   }

   private static string ExtractSupplementalText(
      string rawHtml,
      bool expanded
   )
   {
      var sections = new List<string>();
      var structuredEntityLineLimit = expanded ? int.MaxValue : 80;
      var keyValueLineLimit = expanded ? int.MaxValue : 60;

      var description = ExtractDescription(rawHtml);

      if(!string.IsNullOrWhiteSpace(description))
      {
         sections.Add("Description:");
         sections.Add(description.Trim());
      }

      var structuredEntitySummary = ExtractStructuredEntitySummary(
         rawHtml,
         structuredEntityLineLimit
      );

      if(!string.IsNullOrWhiteSpace(structuredEntitySummary))
      {
         if(sections.Count > 0)
         {
            sections.Add(string.Empty);
         }

         sections.Add("Structured entities:");
         sections.Add(structuredEntitySummary);
      }

      var embeddedData = string.IsNullOrWhiteSpace(structuredEntitySummary)
         ? ExtractEmbeddedDataSections(
            rawHtml,
            expanded,
            keyValueLineLimit
         )
         : [];

      if(embeddedData.Count > 0)
      {
         if(sections.Count > 0)
         {
            sections.Add(string.Empty);
         }

         sections.Add("Embedded data:");
         sections.AddRange(embeddedData);
      }

      if(sections.Count == 0)
      {
         return string.Empty;
      }

      sections.Insert(0, "Page appears to be client-rendered.");

      return string.Join(Environment.NewLine, sections).Trim();
   }

   private static void AddSection(
      List<string> sections,
      string text
   )
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         return;
      }

      var trimmedText = text.Trim();

      if(sections.Contains(trimmedText, StringComparer.Ordinal))
      {
         return;
      }

      sections.Add(trimmedText);
   }

   private static string LimitReturnedText(
      string text,
      int maxLength
   )
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         return string.Empty;
      }

      var trimmedText = text.Trim();

      if(trimmedText.Length > maxLength)
      {
         return AddCutoffMarker(trimmedText[..maxLength].TrimEnd());
      }

      return trimmedText;
   }

   private static string AddCutoffMarker(string text)
   {
      return text + Environment.NewLine + CutOffMarker;
   }

   private static string ExtractSearchText(string rawHtml)
   {
      var candidate = ExtractContentCandidate(rawHtml);
      candidate = NormalizeCountryMarkers(candidate);

      var sections = new List<string>();

      foreach(var text in new[]
      {
         ExtractPlainText(candidate),
         ExtractTableText(candidate),
         ExtractSupplementalText(rawHtml, true)
      })
      {
         if(string.IsNullOrWhiteSpace(text))
         {
            continue;
         }

         sections.Add(text.Trim());
      }

      return string.Join(Environment.NewLine + Environment.NewLine, sections);
   }

   private static string ExtractDescription(string rawHtml)
   {
      var candidates = new[]
      {
         ExtractMetaContent(rawHtml, "name", "description"),
         ExtractMetaContent(rawHtml, "property", "og:description"),
         ExtractMetaContent(rawHtml, "name", "twitter:description"),
         ExtractMetaContent(rawHtml, "itemprop", "description")
      };

      return candidates.FirstOrDefault(
         candidate => !string.IsNullOrWhiteSpace(candidate)
      ) ?? string.Empty;
   }

   private static IReadOnlyList<string> ExtractEmbeddedDataSections(
      string rawHtml,
      bool expanded,
      int keyValueLineLimit
   )
   {
      var sections = new List<string>();

      foreach(var (label, jsonText) in ExtractJsonLdSections(rawHtml))
      {
         sections.Add(FormatJsonSection(label, jsonText));
      }

      foreach(var (label, jsonText) in ExtractInlineJsonSections(rawHtml))
      {
         sections.Add(FormatJsonSection(label, jsonText));
      }

      foreach(var (label, jsonText) in ExtractGenericJsonSections(rawHtml))
      {
         sections.Add(FormatJsonSection(label, jsonText));
      }

      foreach(var (label, jsonText) in ExtractNextFlightSections(rawHtml))
      {
         sections.Add(FormatJsonSection(label, jsonText));
      }

      if(expanded)
      {
         var rawSummary = ExtractMeaningfulKeyValueSummaryAcrossUnescapes(
            rawHtml,
            keyValueLineLimit
         );

         if(!string.IsNullOrWhiteSpace(rawSummary))
         {
            sections.Add(string.Join(
               Environment.NewLine,
               new[]
               {
                  "Unescaped raw values:",
                  rawSummary
               }
            ));
         }
      }

      return sections;
   }

   private static string ExtractStructuredEntitySummary(
      string rawHtml,
      int maxLines
   )
   {
      var lines = new List<string>();
      var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      var cutoff = false;

      var markers = new[]
      {
         "\\\"type\\\":\\\"athlete\\\"",
         "\\\"type\\\":\\\"player\\\"",
         "\\\"type\\\":\\\"team\\\""
      };

      foreach(var marker in markers)
      {
         var searchIndex = 0;

         while(lines.Count < maxLines)
         {
            var matchIndex = rawHtml.IndexOf(
               marker,
               searchIndex,
               StringComparison.OrdinalIgnoreCase
            );

            if(matchIndex < 0)
            {
               break;
            }

            var blockLength = Math.Min(2500, rawHtml.Length - matchIndex);
            var block = rawHtml[matchIndex..(matchIndex + blockLength)];
            var normalizedBlock = NormalizeEscapedText(block);

            var type = marker.Contains(
               "athlete",
               StringComparison.OrdinalIgnoreCase
            )
               ? "athlete"
               : marker.Contains("player", StringComparison.OrdinalIgnoreCase)
                  ? "player"
                  : "team";

            var name = ExtractFirstField(normalizedBlock, "name");
            var shortName = ExtractFirstField(normalizedBlock, "shortName");
            var country = ExtractCountryLabel(normalizedBlock);

            var summary = string.Join(
               " / ",
               new[]
               {
                  name,
                  shortName,
                  country
               }.Where(value => !string.IsNullOrWhiteSpace(value))
            );

            if(!string.IsNullOrWhiteSpace(summary))
            {
               var line = $"{type}: {summary}";

               if(seen.Add(line))
               {
                  lines.Add(line);

                  if(lines.Count >= maxLines)
                  {
                     cutoff = true;
                     break;
                  }
               }
            }

            searchIndex = matchIndex + marker.Length;
         }

         if(cutoff)
         {
            break;
         }
      }

      if(cutoff)
      {
         lines.Add(CutOffMarker);
      }

      return string.Join(Environment.NewLine, lines);
   }

   private static string ExtractCountryLabel(string text)
   {
      var markers = new[]
      {
         "\"country\":{\"altText\":null,\"label\":\"",
         "\"country\":{\"label\":\"",
         "\"label\":\""
      };

      foreach(var marker in markers)
      {
         var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

         if(index < 0)
         {
            continue;
         }

         index += marker.Length;
         var endIndex = text.IndexOf('"', index);

         if(endIndex < 0 || endIndex <= index)
         {
            continue;
         }

         return CleanText(text[index..endIndex]);
      }

      return string.Empty;
   }

   private static string NormalizeEscapedText(string text)
   {
      var current = text;

      for(var iteration = 0; iteration < 3; iteration++)
      {
         if(!TryUnescapeRegexText(current, out var next))
         {
            break;
         }

         if(string.Equals(next, current, StringComparison.Ordinal))
         {
            break;
         }

         current = next;
      }

      return current;
   }

   private static bool TryUnescapeRegexText(
      string text,
      out string unescapedText
   )
   {
      try
      {
         unescapedText = Regex.Unescape(text);
         return true;
      }
      catch(ArgumentException)
      {
         unescapedText = text;
         return false;
      }
   }

   private static string ExtractFirstField(string text, string key)
   {
      var candidates = new[]
      {
         $"\"{key}\":\"",
         $"\\\"{key}\\\":\\\"",
         $"\\\\\"{key}\\\\\":\\\\\""
      };

      foreach(var marker in candidates)
      {
         var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

         if(index < 0)
         {
            continue;
         }

         index += marker.Length;
         var endIndex = text.IndexOf('"', index);

         if(endIndex < 0 || endIndex <= index)
         {
            continue;
         }

         return CleanText(text[index..endIndex]);
      }

      return string.Empty;
   }

   private static string ExtractMeaningfulKeyValueSummaryAcrossUnescapes(
      string text,
      int maxLines
   )
   {
      var candidates = new List<string>();
      var current = text;

      for(var iteration = 0;
         iteration < 4 &&
         !string.IsNullOrWhiteSpace(current);
         iteration++)
      {
         candidates.Add(current);

         if(!TryUnescapeRegexText(current, out var next))
         {
            break;
         }

         if(string.Equals(next, current, StringComparison.Ordinal))
         {
            break;
         }

         current = next;
      }

      var lines = new List<string>();
      var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      var cutoff = false;

      foreach(var candidate in candidates)
      {
         var summary = ExtractMeaningfulKeyValueSummary(candidate);

         if(string.IsNullOrWhiteSpace(summary))
         {
            continue;
         }

         foreach(var line in summary.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
         ))
         {
            if(!seen.Add(line))
            {
               continue;
            }

            lines.Add(line);

            if(lines.Count >= maxLines)
            {
               cutoff = true;
               break;
            }
         }

         if(cutoff)
         {
            break;
         }
      }

      if(cutoff)
      {
         lines.Add(CutOffMarker);
      }

      return string.Join(Environment.NewLine, lines);
   }

   private static IReadOnlyList<(string Label, string JsonText)>
      ExtractJsonLdSections(string rawHtml)
   {
      var sections = new List<(string Label, string JsonText)>();

      foreach(Match match in JsonLdScriptRegex.Matches(rawHtml))
      {
         var jsonText = WebUtility.HtmlDecode(match.Groups["json"].Value)
            .Trim();

         if(string.IsNullOrWhiteSpace(jsonText))
         {
            continue;
         }

         sections.Add(("JSON-LD", jsonText));
      }

      return sections;
   }

   private static IReadOnlyList<(string Label, string JsonText)>
      ExtractNextFlightSections(string rawHtml)
   {
      var sections = new List<(string Label, string JsonText)>();

      foreach(Match match in NextFlightPushRegex.Matches(rawHtml))
      {
         var payload = match.Groups["payload"].Value;

         if(string.IsNullOrWhiteSpace(payload))
         {
            continue;
         }

         var decodedPayload = TryUnescapeRegexText(
            payload,
            out var decodedPayloadValue
         )
            ? decodedPayloadValue
            : payload;
         var lines = new List<string>();

         if(TryExtractJsonFromText(decodedPayload, out var jsonText))
         {
            lines.Add(jsonText);

            var fieldSummary = ExtractInterestingJsonFieldSummary(jsonText);

            if(!string.IsNullOrWhiteSpace(fieldSummary))
            {
               lines.Add(fieldSummary);
            }
         }

         var escapedFieldSummary = ExtractEscapedFieldSummary(payload);

         if(!string.IsNullOrWhiteSpace(escapedFieldSummary))
         {
            lines.Add(escapedFieldSummary);
         }

         var escapedSummary = ExtractMeaningfulEscapedKeyValueSummary(
            payload
         );

         if(!string.IsNullOrWhiteSpace(escapedSummary))
         {
            lines.Add(escapedSummary);
         }

         var summary = ExtractMeaningfulKeyValueSummary(decodedPayload);

         if(!string.IsNullOrWhiteSpace(summary))
         {
            lines.Add(summary);
         }

         if(lines.Count > 0)
         {
            sections.Add(("Next flight", string.Join(
               Environment.NewLine,
               lines
            )));
         }
      }

      return sections;
   }

   private static string ExtractInterestingJsonFieldSummary(string jsonText)
   {
      try
      {
         using var document = JsonDocument.Parse(
            jsonText,
            new JsonDocumentOptions
            {
               AllowTrailingCommas = true,
               CommentHandling = JsonCommentHandling.Skip
            }
         );

         var lines = new List<string>();
         var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

         CollectInterestingJsonFields(
            document.RootElement,
            lines,
            seen,
            0
         );

         return string.Join(Environment.NewLine, lines);
      }
      catch(JsonException)
      {
         return string.Empty;
      }
   }

   private static void CollectInterestingJsonFields(
      JsonElement element,
      List<string> lines,
      HashSet<string> seen,
      int depth
   )
   {
      if(lines.Count >= 40 || depth >= 12)
      {
         return;
      }

      switch(element.ValueKind)
      {
         case JsonValueKind.Object:
            foreach(var property in element.EnumerateObject())
            {
               if(lines.Count >= 40)
               {
                  return;
               }

               if(IsInterestingJsonFieldName(property.Name))
               {
                  CollectInterestingJsonScalar(
                     property.Name,
                     property.Value,
                     lines,
                     seen
                  );
               }

               CollectInterestingJsonFields(
                  property.Value,
                  lines,
                  seen,
                  depth + 1
               );
            }

            break;
         case JsonValueKind.Array:
            foreach(var item in element.EnumerateArray())
            {
               CollectInterestingJsonFields(item, lines, seen, depth + 1);
            }

            break;
         case JsonValueKind.String:
         {
            var nestedJson = element.GetString();

            if(string.IsNullOrWhiteSpace(nestedJson))
            {
               break;
            }

            var nestedCandidates = new List<string>();
            var currentCandidate = nestedJson;

            for(var iteration = 0;
               iteration < 4 &&
               !string.IsNullOrWhiteSpace(currentCandidate);
               iteration++)
            {
               nestedCandidates.Add(currentCandidate);

               if(!TryUnescapeRegexText(
                  currentCandidate,
                  out var nextCandidate
               ))
               {
                  break;
               }

               if(string.Equals(
                  nextCandidate,
                  currentCandidate,
                  StringComparison.Ordinal
               ))
               {
                  break;
               }

               currentCandidate = nextCandidate;
            }

            foreach(var nestedCandidate in nestedCandidates)
            {
               if(!TryParseJsonText(nestedCandidate, out var nestedJsonText))
               {
                  continue;
               }

               try
               {
                  using var nestedDocument = JsonDocument.Parse(
                     nestedJsonText,
                     new JsonDocumentOptions
                     {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip
                     }
                  );

                  CollectInterestingJsonFields(
                     nestedDocument.RootElement,
                     lines,
                     seen,
                     depth + 1
                  );
               }
               catch(JsonException)
               {
               }
            }

            break;
         }
      }
   }

   private static void CollectInterestingJsonScalar(
      string name,
      JsonElement value,
      List<string> lines,
      HashSet<string> seen
   )
   {
      if(value.ValueKind != JsonValueKind.String)
      {
         return;
      }

      var text = CleanText(value.GetString() ?? string.Empty);

      if(string.IsNullOrWhiteSpace(text))
      {
         return;
      }

      if(text.Length > 160)
      {
         text = text[..160].TrimEnd() + "...";
      }

      var signature = $"{name}:{text}";

      if(!seen.Add(signature))
      {
         return;
      }

      lines.Add($"{name}: {text}");
   }

   private static bool IsInterestingJsonFieldName(string name)
   {
      return name.ToLowerInvariant() is
         "name" or
         "title" or
         "label" or
         "text" or
         "description" or
         "country" or
         "shortname" or
         "alttext";
   }

   private static string ExtractEscapedFieldSummary(string payload)
   {
      var keys = new[]
      {
         "name",
         "title",
         "label",
         "text",
         "shortName",
         "country"
      };
      var lines = new List<string>();
      var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach(var key in keys)
      {
         var search = $"\\\"{key}\\\":\\\"";
         var index = payload.IndexOf(search, StringComparison.Ordinal);

         if(index < 0)
         {
            continue;
         }

         index += search.Length;
         var endIndex = payload.IndexOf("\\\"", index, StringComparison.Ordinal);

         if(endIndex < 0 || endIndex <= index)
         {
            continue;
         }

         var value = WebUtility.HtmlDecode(payload[index..endIndex]).Trim();

         if(string.IsNullOrWhiteSpace(value))
         {
            continue;
         }

         var signature = $"{key}:{value}";

         if(!seen.Add(signature))
         {
            continue;
         }

         lines.Add($"{key}: {value}");

         if(lines.Count >= 30)
         {
            break;
         }
      }

      return string.Join(Environment.NewLine, lines);
   }

   private static string ExtractMeaningfulEscapedKeyValueSummary(string text)
   {
      var matches = Regex.Matches(
         text,
         "\\\\\\\"?(?<key>name|title|label|text|description|country|" +
         "shortName)\\\\\\\"?\\s*:\\s*\\\\\\\"(?<value>[^\\\\\\\"]{1,160})" +
         "\\\\\\\"",
         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
      );

      return ExtractMeaningfulKeyValueSummaryFromMatches(matches, 30);
   }

   private static string ExtractMeaningfulKeyValueSummary(string text)
   {
      var matches = Regex.Matches(
         text,
         @"""?(?<key>name|title|label|text|description|country|shortName)""?" +
         @"\s*:\s*""(?<value>[^""]{1,160})""",
         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
      );
      return ExtractMeaningfulKeyValueSummaryFromMatches(matches, 30);
   }

   private static string ExtractMeaningfulKeyValueSummaryFromMatches(
      MatchCollection matches,
      int maxLines
   )
   {
      var entries = new List<(int Score, string Line, string Signature)>();
      var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach(Match match in matches)
      {
         var key = match.Groups["key"].Value.Trim();
         var value = CleanText(match.Groups["value"].Value);

         if(string.IsNullOrWhiteSpace(key) ||
            string.IsNullOrWhiteSpace(value))
         {
            continue;
         }

         var signature = $"{key}:{value}";

         if(!seen.Add(signature))
         {
            continue;
         }

         var score = ScoreMeaningfulKeyValue(key, value);
         entries.Add((score, $"{key}: {value}", signature));
      }

      var lines = entries
         .OrderByDescending(entry => entry.Score)
         .ThenBy(entry => entry.Line, StringComparer.OrdinalIgnoreCase)
         .Take(maxLines)
         .Select(entry => entry.Line)
         .ToArray();

      if(entries.Count > maxLines)
      {
         return string.Join(
            Environment.NewLine,
            lines.Concat([CutOffMarker])
         );
      }

      return string.Join(Environment.NewLine, lines);
   }

   private static int ScoreMeaningfulKeyValue(string key, string value)
   {
      var score = 0;
      var normalizedKey = key.Trim().ToLowerInvariant();
      var normalizedValue = value.Trim();

      if(string.IsNullOrWhiteSpace(normalizedKey) ||
         string.IsNullOrWhiteSpace(normalizedValue))
      {
         return int.MinValue;
      }

      switch(normalizedKey)
      {
         case "name":
         case "title":
            score += 20;
            break;
         case "shortname":
            score += 18;
            break;
         case "country":
         case "label":
            score += 16;
            break;
         case "text":
            score += 8;
            break;
         case "description":
            score += 6;
            break;
      }

      if(LooksLikeCountryCode(normalizedValue))
      {
         score += 18;
      }

      if(LooksLikePersonName(normalizedValue))
      {
         score += 14;
      }

      if(normalizedValue.Contains('/'))
      {
         score -= 8;
      }

      if(normalizedValue.Contains('-') &&
         normalizedValue.All(ch => char.IsLower(ch) || ch == '-'))
      {
         score -= 6;
      }

      if(normalizedValue.Length <= 3 && !LooksLikeCountryCode(normalizedValue))
      {
         score -= 8;
      }

      if(normalizedValue.All(ch => !char.IsLetter(ch) || char.IsUpper(ch)))
      {
         score += 4;
      }

      return score;
   }

   private static bool LooksLikeCountryCode(string value)
   {
      return value.Length is >= 2 and <= 4 &&
         value.All(ch => char.IsUpper(ch) || char.IsDigit(ch));
   }

   private static bool LooksLikePersonName(string value)
   {
      var parts = value.Split(
         ' ',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      );

      if(parts.Length < 2)
      {
         return false;
      }

      return parts.All(part =>
         part.Length > 1 &&
         (char.IsUpper(part[0]) || char.IsDigit(part[0])) &&
         part.Skip(1).All(ch => char.IsLower(ch) || ch == '.' || ch == '-')
      );
   }

   private static IReadOnlyList<(string Label, string JsonText)>
      ExtractGenericJsonSections(string rawHtml)
   {
      var sections = new List<(string Label, string JsonText)>();

      foreach(Match match in ScriptRegex.Matches(rawHtml))
      {
         var script = WebUtility.HtmlDecode(match.Groups["script"].Value)
            .Trim();

         if(string.IsNullOrWhiteSpace(script))
         {
            continue;
         }

         if(TryExtractJsonFromScript(script, out var jsonText))
         {
            sections.Add(("Script JSON", jsonText));
         }
      }

      return sections;
   }

   private static bool TryExtractJsonFromScript(
      string script,
      out string jsonText
   )
   {
      jsonText = string.Empty;

      if(TryExtractJsonFromText(script, out jsonText))
      {
         return true;
      }

      if(!TryUnescapeRegexText(script, out var unescapedScript))
      {
         return false;
      }

      if(!string.Equals(unescapedScript, script, StringComparison.Ordinal)
         && TryExtractJsonFromText(unescapedScript, out jsonText))
      {
         return true;
      }

      return false;
   }

   private static bool TryExtractJsonFromText(
      string text,
      out string jsonText
   )
   {
      jsonText = string.Empty;

      if(TryParseJsonText(text, out jsonText))
      {
         return true;
      }

      for(var index = 0; index < text.Length; index++)
      {
         var ch = text[index];

         if(ch != '{' && ch != '[')
         {
            continue;
         }

         if(!TryExtractBalancedJson(text, index, out var candidate))
         {
            continue;
         }

         if(TryParseJsonText(candidate, out jsonText))
         {
            return true;
         }
      }

      return false;
   }

   private static bool TryParseJsonText(string jsonText, out string normalized)
   {
      normalized = string.Empty;

      try
      {
         using var document = JsonDocument.Parse(
            jsonText,
            new JsonDocumentOptions
            {
               AllowTrailingCommas = true,
               CommentHandling = JsonCommentHandling.Skip
            }
         );

         normalized = document.RootElement.GetRawText();
         return true;
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static bool TryExtractBalancedJson(
      string text,
      int startIndex,
      out string jsonText
   )
   {
      jsonText = string.Empty;

      if(startIndex < 0 || startIndex >= text.Length)
      {
         return false;
      }

      var open = text[startIndex];
      var close = open == '{' ? '}' : open == '[' ? ']' : '\0';

      if(close == '\0')
      {
         return false;
      }

      var depth = 0;
      var inString = false;
      var escape = false;

      for(var index = startIndex; index < text.Length; index++)
      {
         var ch = text[index];

         if(inString)
         {
            if(escape)
            {
               escape = false;
               continue;
            }

            if(ch == '\\')
            {
               escape = true;
               continue;
            }

            if(ch == '"')
            {
               inString = false;
            }

            continue;
         }

         if(ch == '"')
         {
            inString = true;
            continue;
         }

         if(ch == open)
         {
            depth++;
         }
         else if(ch == close)
         {
            depth--;

            if(depth == 0)
            {
               jsonText = text[startIndex..(index + 1)];
               return true;
            }
         }
      }

      return false;
   }

   private static IReadOnlyList<(string Label, string JsonText)>
      ExtractInlineJsonSections(string rawHtml)
   {
      var sections = new List<(string Label, string JsonText)>();

      foreach(var (marker, label) in InlineJsonMarkers)
      {
         foreach(Match match in ScriptRegex.Matches(rawHtml))
         {
            var script = WebUtility.HtmlDecode(match.Groups["script"].Value);
            var jsonText = ExtractAssignedJson(script, marker);

            if(string.IsNullOrWhiteSpace(jsonText))
            {
               continue;
            }

            sections.Add((label, jsonText));
            break;
         }
      }

      return sections;
   }

   private static readonly (string Marker, string Label)[] InlineJsonMarkers =
   {
      ("window.__SITE_SETTINGS__", "Site settings"),
      ("window.__INITIAL_STATE__", "Initial state"),
      ("window.__PRELOADED_STATE__", "Preloaded state"),
      ("window.__NEXT_DATA__", "Next data"),
      ("window.__NUXT__", "Nuxt data")
   };

   private static string? ExtractAssignedJson(
      string script,
      string marker
   )
   {
      var markerIndex = script.IndexOf(marker, StringComparison.Ordinal);

      if(markerIndex < 0)
      {
         return null;
      }

      var value = script[(markerIndex + marker.Length)..].TrimStart();

      if(value.StartsWith("="))
      {
         value = value[1..].TrimStart();
      }

      value = value.Trim();

      if(value.EndsWith(';'))
      {
         value = value[..^1].TrimEnd();
      }

      if(value.StartsWith("{", StringComparison.Ordinal) ||
         value.StartsWith("[", StringComparison.Ordinal))
      {
         return value;
      }

      return null;
   }

   private static string FormatJsonSection(string label, string jsonText)
   {
      if(TryFormatJson(jsonText, out var formattedJson))
      {
         return string.Join(
            Environment.NewLine,
            new[]
            {
               $"{label}:",
               formattedJson
            }
         );
      }

      var snippet = CleanText(jsonText);

      if(snippet.Length > 1200)
      {
         snippet = snippet[..1200].TrimEnd() + "...";
      }

      return string.Join(
         Environment.NewLine,
         new[]
         {
            $"{label}:",
            snippet
         }
      );
   }

   private static bool TryFormatJson(
      string jsonText,
      out string formattedJson
   )
   {
      formattedJson = string.Empty;

      try
      {
         using var document = JsonDocument.Parse(
            jsonText,
            new JsonDocumentOptions
            {
               AllowTrailingCommas = true,
               CommentHandling = JsonCommentHandling.Skip
            }
         );

         formattedJson = SummarizeJsonElement(document.RootElement, 0);
         return !string.IsNullOrWhiteSpace(formattedJson);
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static string SummarizeJsonElement(
      JsonElement element,
      int depth
   )
   {
      var lines = new List<string>();
      AppendJsonSummary(lines, element, string.Empty, depth);
      return string.Join(Environment.NewLine, lines);
   }

   private static void AppendJsonSummary(
      List<string> lines,
      JsonElement element,
      string indent,
      int depth
   )
   {
      const int maxProperties = 12;
      const int maxArrayItems = 3;

      switch(element.ValueKind)
      {
         case JsonValueKind.Object:
         {
            var properties = element
               .EnumerateObject()
               .Where(property => !IsNoiseProperty(property.Name))
               .OrderByDescending(property => ScoreJsonProperty(property))
               .ThenBy(
                  property => property.Name,
                  StringComparer.OrdinalIgnoreCase
               )
               .ToArray();
            var propertyCount = 0;

            foreach(var property in properties)
            {
               if(propertyCount >= maxProperties)
               {
                  lines.Add($"{indent}...");
                  break;
               }

               propertyCount++;

               AppendJsonPropertySummary(
                  lines,
                  property.Name,
                  property.Value,
                  indent,
                  depth,
                  maxArrayItems
               );
            }

            return;
         }
         case JsonValueKind.Array:
         {
            AppendJsonArraySummary(lines, element, indent, depth, maxArrayItems);
            return;
         }
         default:
         {
            lines.Add($"{indent}{FormatJsonScalar(element)}");
            return;
         }
      }
   }

   private static void AppendJsonPropertySummary(
      List<string> lines,
      string name,
      JsonElement value,
      string indent,
      int depth,
      int maxArrayItems
   )
   {
      var nextIndent = string.IsNullOrEmpty(indent)
         ? "  "
         : indent + "  ";

      switch(value.ValueKind)
      {
         case JsonValueKind.Object:
            lines.Add($"{indent}- {name}:");

            if(depth >= 10)
            {
               lines.Add($"{nextIndent}...");
               return;
            }

            AppendJsonSummary(lines, value, nextIndent, depth + 1);
            return;
         case JsonValueKind.Array:
            lines.Add($"{indent}- {name}:");

            if(depth >= 10)
            {
               lines.Add($"{nextIndent}...");
               return;
            }

            AppendJsonArraySummary(
               lines,
               value,
               nextIndent,
               depth + 1,
               maxArrayItems
            );
            return;
         default:
            lines.Add($"{indent}- {name}: {FormatJsonScalar(value)}");
            return;
      }
   }

   private static void AppendJsonArraySummary(
      List<string> lines,
      JsonElement array,
      string indent,
      int depth,
      int maxArrayItems
   )
   {
      var itemCount = 0;

      foreach(var item in array.EnumerateArray())
      {
         if(itemCount >= maxArrayItems)
         {
            lines.Add($"{indent}...");
            break;
         }

         itemCount++;

         if(item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
         {
            lines.Add($"{indent}-");

            if(depth >= 10)
            {
               lines.Add($"{indent}  ...");
               continue;
            }

            AppendJsonSummary(lines, item, indent + "  ", depth + 1);
            continue;
         }

         lines.Add($"{indent}- {FormatJsonScalar(item)}");
      }

      if(itemCount == 0)
      {
         lines.Add($"{indent}[]");
      }
   }

   private static string FormatJsonArray(
      JsonElement array,
      int maxArrayItems
   )
   {
      if(array.GetArrayLength() == 0)
      {
         return "[]";
      }

      var values = new List<string>();
      var count = 0;

      foreach(var item in array.EnumerateArray())
      {
         if(count >= maxArrayItems)
         {
            values.Add("...");
            break;
         }

         count++;
         values.Add(FormatJsonScalar(item));
      }

      return $"[{string.Join(", ", values)}]";
   }

   private static string FormatJsonScalar(JsonElement element)
   {
      return element.ValueKind switch
      {
         JsonValueKind.String => TruncateForSummary(element.GetString() ?? ""),
         JsonValueKind.Number => element.GetRawText(),
         JsonValueKind.True => "true",
         JsonValueKind.False => "false",
         JsonValueKind.Null => "null",
         _ => TruncateForSummary(element.GetRawText())
      };
   }

   private static bool IsNoiseProperty(string name)
   {
      return string.Equals(name, "$schema", StringComparison.OrdinalIgnoreCase);
   }

   private static int ScoreJsonProperty(JsonProperty property)
   {
      var score = 0;
      var name = property.Name.Trim();

      if(string.IsNullOrWhiteSpace(name))
      {
         return score;
      }

      if(property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
      {
         score += 4;
      }

      switch(name.ToLowerInvariant())
      {
         case "name":
         case "title":
         case "label":
         case "text":
         case "description":
         case "country":
         case "athlete":
         case "athletes":
         case "player":
         case "players":
         case "item":
         case "items":
         case "row":
         case "rows":
         case "data":
         case "content":
         case "children":
            score += 10;
            break;
         case "props":
         case "page":
         case "pages":
         case "section":
         case "sections":
         case "table":
         case "tables":
            score += 6;
            break;
         case "theme":
         case "config":
         case "style":
         case "styles":
         case "buildid":
         case "assetprefix":
         case "urlparts":
         case "initialtree":
         case "pageTags":
            score -= 4;
            break;
      }

      return score;
   }

   private static string TruncateForSummary(string value)
   {
      var cleaned = CleanText(value);

      if(cleaned.Length <= 200)
      {
         return cleaned;
      }

      return cleaned[..200].TrimEnd() + "...";
   }

   private static IReadOnlyList<string> ExtractHeadings(string rawHtml)
   {
      var candidate = ExtractContentCandidate(rawHtml);
      var headings = new List<string>();

      foreach(Match match in HeadingRegex.Matches(candidate))
      {
         var headingHtml = match.Groups["text"].Value;

         if(NoisyHeadingTagRegex.IsMatch(headingHtml))
         {
            continue;
         }

         var heading = CleanText(headingHtml);

         if(string.IsNullOrWhiteSpace(heading))
         {
            continue;
         }

         if(heading.Length > 120)
         {
            continue;
         }

         if(!headings.Contains(heading, StringComparer.OrdinalIgnoreCase))
         {
            headings.Add(heading);
         }
      }

      return headings;
   }

   private static string ExtractContentCandidate(string rawHtml)
   {
      return ExtractSection(rawHtml, ArticleRegex)
         ?? ExtractSection(rawHtml, MainRegex)
         ?? ExtractSection(rawHtml, BodyRegex)
         ?? rawHtml;
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

   private static IReadOnlyList<string> ExtractFlagLabels(string rawHtml)
   {
      var labels = new List<string>();

      foreach(Match match in CountryElementRegex.Matches(rawHtml))
      {
         var element = match.Value;
         AddLabelFromAttribute(labels, element, "aria-label");
         AddLabelFromAttribute(labels, element, "title");
         AddLabelFromAttribute(labels, element, "alt");
         AddLabelFromAttribute(labels, element, "data-country");
         AddLabelFromAttribute(labels, element, "data-country-code");
         AddLabelFromAttribute(labels, element, "data-country-name");
         AddLabelFromAttribute(labels, element, "data-iso");

         var classValue = ExtractAttributeValue(element, "class");

         foreach(var token in classValue.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries
         ))
         {
            if(TryGetCountryLabelFromClassToken(token, out var label))
            {
               AddUniqueLabel(labels, label);
            }
         }
      }

      return labels;
   }

   private static void AddLabelFromAttribute(
      ICollection<string> labels,
      string element,
      string attributeName
   )
   {
      var value = ExtractAttributeValue(element, attributeName);

      if(string.IsNullOrWhiteSpace(value))
      {
         return;
      }

      if(TryGetCountryLabelFromRawValue(value, out var label))
      {
         AddUniqueLabel(labels, label);
      }
   }

   private static string NormalizeCountryMarkers(string html)
   {
      return CountryElementRegex.Replace(
         html,
         match =>
         {
            var element = match.Value;

            if(TryExtractCountryLabelFromElement(element, out var label))
            {
               return " " + label + " ";
            }

            return element;
         }
      );
   }

   private static bool TryExtractCountryLabelFromElement(
      string element,
      out string label
   )
   {
      label = "";

      AddLabelFromAttributeCandidate(element, "aria-label", ref label);
      AddLabelFromAttributeCandidate(element, "title", ref label);
      AddLabelFromAttributeCandidate(element, "alt", ref label);
      AddLabelFromAttributeCandidate(element, "data-country", ref label);
      AddLabelFromAttributeCandidate(
         element,
         "data-country-code",
         ref label
      );
      AddLabelFromAttributeCandidate(
         element,
         "data-country-name",
         ref label
      );
      AddLabelFromAttributeCandidate(element, "data-iso", ref label);

      if(!string.IsNullOrWhiteSpace(label))
      {
         return true;
      }

      var classValue = ExtractAttributeValue(element, "class");

      foreach(var token in classValue.Split(
         ' ',
         StringSplitOptions.RemoveEmptyEntries |
         StringSplitOptions.TrimEntries
      ))
      {
         if(TryGetCountryLabelFromClassToken(token, out label))
         {
            return true;
         }
      }

      return false;
   }

   private static void AddLabelFromAttributeCandidate(
      string element,
      string attributeName,
      ref string label
   )
   {
      if(!string.IsNullOrWhiteSpace(label))
      {
         return;
      }

      var value = ExtractAttributeValue(element, attributeName);

      if(string.IsNullOrWhiteSpace(value))
      {
         return;
      }

      if(TryGetCountryLabelFromRawValue(value, out label))
      {
         return;
      }
   }

   private static void AddUniqueLabel(
      ICollection<string> labels,
      string label
   )
   {
      if(string.IsNullOrWhiteSpace(label) ||
         labels.Contains(label, StringComparer.OrdinalIgnoreCase))
      {
         return;
      }

      labels.Add(label);
   }

   private static string ExtractAttributeValue(
      string element,
      string attributeName
   )
   {
      var pattern =
         $@"\b{Regex.Escape(attributeName)}\s*=\s*[""'](?<value>[^""']+)[""']";

      var match = Regex.Match(
         element,
         pattern,
         RegexOptions.IgnoreCase |
         RegexOptions.Singleline |
         RegexOptions.CultureInvariant
      );

      return match.Success
         ? WebUtility.HtmlDecode(match.Groups["value"].Value).Trim()
         : "";
   }

   private static bool TryGetCountryLabelFromClassToken(
      string token,
      out string label
   )
   {
      label = "";

      if(TryGetCountryName(token, out label))
      {
         return true;
      }

      foreach(var separator in new[] { '-', '_' })
      {
         var separatorIndex = token.LastIndexOf(separator);

         if(separatorIndex < 0 || separatorIndex == token.Length - 1)
         {
            continue;
         }

         var suffix = token[(separatorIndex + 1)..];

         if(TryGetCountryName(suffix, out label))
         {
            return true;
         }
      }

      return false;
   }

   private static bool TryGetCountryLabelFromRawValue(
      string value,
      out string label
   )
   {
      label = "";

      var cleaned = CleanText(value);

      if(string.IsNullOrWhiteSpace(cleaned))
      {
         return false;
      }

      if(TryGetCountryName(cleaned, out label))
      {
         return true;
      }

      if(TryGetCountryLabelByName(cleaned, out label))
      {
         return true;
      }

      var stripped = StripCountryDecorators(cleaned);

      if(stripped.Length > 0 && TryGetCountryName(stripped, out label))
      {
         return true;
      }

      if(stripped.Length > 0 && TryGetCountryLabelByName(stripped, out label))
      {
         return true;
      }

      return false;
   }

   private static string StripCountryDecorators(string value)
   {
      var stripped = Regex.Replace(
         value,
         @"\b(flag|country|national|team)\b",
         "",
         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
      );

      return stripped
         .Replace("  ", " ")
         .Trim(' ', '-', '_', '|', '/', '\\');
   }

   private static bool TryGetCountryName(
      string code,
      out string label
   )
   {
      label = "";

      try
      {
         var region = new RegionInfo(code.ToUpperInvariant());
         label = region.EnglishName;
         return true;
      }
      catch(ArgumentException)
      {
         return false;
      }
   }

   private static bool TryGetCountryLabelByName(
      string value,
      out string label
   )
   {
      label = "";

      var key = value.Trim().ToLowerInvariant();
      if(!CountryNameLookup.Value.TryGetValue(key, out var matchedLabel))
      {
         return false;
      }

      label = matchedLabel;
      return true;
   }

   private static IReadOnlyDictionary<string, string> BuildCountryNameLookup()
   {
      var lookup = new Dictionary<string, string>(StringComparer.Ordinal);

      foreach(var culture in CultureInfo
         .GetCultures(CultureTypes.SpecificCultures))
      {
         try
         {
            var region = new RegionInfo(culture.Name);
            var key = region.EnglishName.Trim().ToLowerInvariant();

            if(!lookup.ContainsKey(key))
            {
               lookup[key] = region.EnglishName;
            }
         }
         catch(ArgumentException)
         {
         }
      }

      return lookup;
   }

   private sealed record MainTextResult(
      string Text,
      bool HasBodyText,
      string FullText
   );
}
