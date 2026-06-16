using System.Net;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

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
         mainTextInfo.HasBodyText
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
      var candidate = ExtractContentCandidate(rawHtml);
      candidate = NormalizeCountryMarkers(candidate);

      var textCandidates = new List<string>
      {
         ExtractPlainText(candidate),
         ExtractTableText(candidate)
      };

      var text = textCandidates
         .OrderByDescending(ScoreText)
         .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text)) ?? "";

      if(!string.IsNullOrWhiteSpace(text))
      {
         if(text.Length > MaxMainTextLength)
         {
            return new MainTextResult(
               text[..MaxMainTextLength].TrimEnd() + "...",
               true
            );
         }

         return new MainTextResult(text.Trim(), true);
      }

      return new MainTextResult(ExtractSupplementalText(rawHtml), false);
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

      if(trimmed.Length == 0)
      {
         return true;
      }

      if(CssNoiseLineRegex.IsMatch(trimmed))
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

      return false;
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
      }

      return score;
   }

   private static string ExtractSupplementalText(string rawHtml)
   {
      var sections = new List<string>();

      var description = ExtractDescription(rawHtml);

      if(!string.IsNullOrWhiteSpace(description))
      {
         sections.Add("Description:");
         sections.Add(description.Trim());
      }

      var embeddedData = ExtractEmbeddedDataSections(rawHtml);

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

      var text = string.Join(Environment.NewLine, sections);

      if(text.Length > MaxMainTextLength)
      {
         return text[..MaxMainTextLength].TrimEnd() + "...";
      }

      return text.Trim();
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
      string rawHtml
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

      return sections;
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
      const int maxProperties = 8;
      const int maxArrayItems = 3;

      switch(element.ValueKind)
      {
         case JsonValueKind.Object:
         {
            var propertyCount = 0;

            foreach(var property in element.EnumerateObject())
            {
               if(propertyCount >= maxProperties)
               {
                  lines.Add($"{indent}...");
                  break;
               }

               propertyCount++;

               if(IsNoiseProperty(property.Name))
               {
                  continue;
               }

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
            var itemCount = 0;

            foreach(var item in element.EnumerateArray())
            {
               if(itemCount >= maxArrayItems)
               {
                  lines.Add($"{indent}...");
                  break;
               }

               itemCount++;
               lines.Add($"{indent}- {FormatJsonScalar(item)}");
            }

            if(itemCount == 0)
            {
               lines.Add($"{indent}[]");
            }

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

            if(depth >= 1)
            {
               lines.Add($"{nextIndent}...");
               return;
            }

            AppendJsonSummary(lines, value, nextIndent, depth + 1);
            return;
         case JsonValueKind.Array:
            lines.Add(
               $"{indent}- {name}: {FormatJsonArray(value, maxArrayItems)}"
            );
            return;
         default:
            lines.Add($"{indent}- {name}: {FormatJsonScalar(value)}");
            return;
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
      bool HasBodyText
   );
}
