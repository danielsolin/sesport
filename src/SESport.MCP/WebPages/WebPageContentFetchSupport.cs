using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Playwright;

using SESport.Core.Formatting;

using UglyToad.PdfPig;

namespace SESport.AI.WebPages;

internal static class WebPageContentFetchSupport
{
   internal static readonly IReadOnlyDictionary<string, string>
      CountryNamesByCode = BuildCountryNamesByCode();
   internal static readonly IReadOnlyDictionary<string, string>
      CountryNamesByThreeLetterCode = BuildCountryNamesByThreeLetterCode();
   private static readonly Lazy<Task<string>> BrowserUserAgentTask =
      new(BuildBrowserUserAgentAsync);
   private static readonly Regex StandaloneNoiseLineRegex = new(
      @"^(?:\d+|[^\p{L}\p{N}]+)$",
      RegexOptions.CultureInvariant
   );
   private static readonly Regex HtmlAnchorRegex = new(
      @"<a\b(?<attrs>[^>]*)>(?<content>.*?)</a>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlImageRegex = new(
      @"<img\b(?<attrs>[^>]*)>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlEmptyFlagElementRegex = new(
      @"<(?<tag>span|i|div)\b(?<attrs>[^>]*)>\s*</\k<tag>>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlSvgRegex = new(
      @"<svg\b(?<attrs>[^>]*)>(?<content>.*?)</svg>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlUseRegex = new(
      @"<use\b(?<attrs>[^>]*)>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlLineBreakTagRegex = new(
      @"<br\b[^>]*>|</(?:address|article|blockquote|div|h[1-6]|" +
      @"li|main|p|section|tr)>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlHeadingRegex = new(
      @"<h(?<level>[1-6])\b[^>]*>(?<content>.*?)</h\k<level>>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlMetaRegex = new(
      @"<meta\b(?<attrs>[^>]*)>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlTimeRegex = new(
      @"<time\b(?<attrs>[^>]*)>(?<content>.*?)</time>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlScriptRegex = new(
      @"<script\b(?<attrs>[^>]*)>(?<content>.*?)</script>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlTagRegex = new(
      @"</?(?<name>[a-zA-Z][a-zA-Z0-9:-]*)(?<attrs>[^>]*)>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex TemplatePlaceholderRegex = new(
      @"\{\{[^{}\r\n]{1,200}\}\}",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex TemplateDirectiveRegex = new(
      @"(?<![\p{L}\p{N}])(?:v-[\w:-]+|:[\w:-]+|@[\w:-]+)" +
      @"\s*(?:=\s*)?(?:""[^""\r\n]*""|'[^'\r\n]*'|[^\s]+)?",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex PlaceholderHeadingRegex = new(
      @"^header\s+\d+$",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex PlaceholderHeadingLineRegex = new(
      @"^\s*header\s+\d+\s*$",
      RegexOptions.IgnoreCase | RegexOptions.Multiline |
         RegexOptions.CultureInvariant | RegexOptions.Compiled
   );
   private static readonly Regex IncompleteContentMarkerRegex = new(
      @"\b(?:TBD|Loading(?:\s*\.\.\.)?|No\s+(?:data|results))\b",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex HtmlAttributeRegex = new(
      @"\b(?<name>[a-zA-Z0-9:-]+)\s*=\s*(?:" +
      @"""(?<value>[^""]*)""|'(?<value>[^']*)')",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlHrefRegex = new(
      @"\bhref\s*=\s*(?:""(?<value>[^""]*)""|" +
      @"'(?<value>[^']*)')",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlBoilerplateBlockRegex = new(
      @"<(?<tag>header|nav|footer|aside|script|style|noscript)\b" +
      @"[^>]*>.*?</\k<tag>>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlSelectBlockRegex = new(
      @"<select\b[^>]*>.*?</select>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlMainBlockRegex = new(
      @"<main\b[^>]*>(?<content>.*?)</main>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlBodyBlockRegex = new(
      @"<body\b[^>]*>(?<content>.*?)</body>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex GenericLinkTextRegex = new(
      @"^(?:more|read more|view all|open|close|next|previous|back|" +
      @"home|menu|search|login|log in|sign in|register|continue|" +
      @"share|print|contact|privacy|terms|cookies?|accessibility)$",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex RelevantLinkLabelBoostRegex = new(
      @"\b(?:entry|entries|start|result|results|schedule|draw|" +
      @"list|live|ranking|registration|entry list|start list)\b",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex FlagSourceCodeRegex = new(
      @"(?:^|[^a-z0-9])flag(?:s)?(?:[-_/#]*)(?<code>[a-z]{2,3})" +
      @"(?:[^a-z0-9]|$)|" +
      @"(?:^|[^a-z0-9])(?<code>[a-z]{2,3})(?:[-_/#]*)(?:flag)(?:s)?" +
      @"(?:[^a-z0-9]|$)|" +
      @"(?:^|[^a-z0-9])(?<code>[a-z]{2,3})(?:[_-]\d|\.)",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex FlagClassCodeRegex = new(
      @"(?:^|\s)flag(?:-icon)?[-_](?<code>[a-z]{2,3})(?:\s|$)",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex FlagNamedCountryRegex = new(
      @"(?:^|[^a-z0-9])Flag_of_(?<country>[A-Za-z_]+)" +
      @"(?:[^a-z0-9]|$)",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex FlagNoisePrefixRegex = new(
      @"^(?:flag|flags)(?:\s+(?:of|for|from))?\s+",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   private static readonly Regex FlagNoiseSuffixRegex = new(
      @"\s+(?:flag|flags)(?:\s+(?:icon|image|symbol))?$",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
         RegexOptions.Compiled
   );
   internal static async Task<string> GetBrowserUserAgentAsync()
   {
      try
      {
         return await BrowserUserAgentTask.Value;
      }
      catch
      {
         return WebPageFetchDefaults.BrowserUserAgentFallback;
      }
   }

   internal static string BuildBrowserUserAgent(string browserVersion)
   {
      var majorVersionMatch = Regex.Match(
         browserVersion,
         @"\b(\d+)",
         RegexOptions.CultureInvariant
      );

      if(!majorVersionMatch.Success ||
         !int.TryParse(
            majorVersionMatch.Groups[1].Value,
            out var majorVersion
         ) ||
         majorVersion <= 0)
      {
         return WebPageFetchDefaults.BrowserUserAgentFallback;
      }

      return
         WebPageFetchDefaults.BrowserUserAgentPrefix +
         majorVersion +
         WebPageFetchDefaults.BrowserUserAgentSuffix;
   }

   internal static IReadOnlyDictionary<string, string>
      BuildBrowserLikeHeaders(string browserUserAgent)
   {
      return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      {
         ["Accept"] = WebPageFetchDefaults.BrowserAcceptHeader,
         ["Accept-Language"] = WebPageFetchDefaults
            .BrowserAcceptLanguageHeader,
         ["Upgrade-Insecure-Requests"] = "1",
         ["Sec-CH-UA"] = BuildSecChUaHeader(browserUserAgent),
         ["Sec-CH-UA-Mobile"] = "?0",
         ["Sec-CH-UA-Platform"] = $"\"{WebPageFetchDefaults.BrowserPlatform}\""
      };
   }

   internal static string NormalizeText(string? text)
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         return string.Empty;
      }

      text = UnicodeTextSanitizer.Sanitize(text);
      text = NormalizeGluedTableCellText(text);

      var normalizedLines = text.Replace("\r", "\n", StringComparison.Ordinal)
         .Split('\n', StringSplitOptions.RemoveEmptyEntries)
         .Select(line => line.Trim())
         .Where(line => !IsStandaloneNoiseLine(line));
      var augmentedLines = CollapseAdjacentNameFragmentRuns(normalizedLines);
      var normalizedText = string.Join(
         Environment.NewLine,
         augmentedLines
      ).Trim();

      return CollapseAdjacentCountryNameDuplicates(normalizedText);
   }

   internal static string NormalizeGluedTableCellText(string text)
   {
      return WebPageTextNormalization.NormalizeGluedTableCellText(text);
   }

   internal static string ApplyResponseCutoff(string text)
   {
      if(string.IsNullOrWhiteSpace(text) ||
         text.Length <= WebPageFetchDefaults.MaxResponseCharacters)
      {
         return text;
      }

      var cutoffLength = WebPageFetchDefaults.MaxResponseCharacters -
         WebPageFetchDefaults.CutoffMarker.Length;

      if(cutoffLength <= 0)
      {
         return WebPageFetchDefaults.CutoffMarker;
      }

      return text[..cutoffLength].TrimEnd() +
         WebPageFetchDefaults.CutoffMarker;
   }

   internal static bool IsPdfResponse(
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

      if(string.Equals(
         contentType,
         "application/x-pdf",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return true;
      }

      return absoluteUrl.AbsolutePath.EndsWith(
         ".pdf",
         StringComparison.OrdinalIgnoreCase
      );
   }

   internal static WebPageContent BuildFailureContent(
      Uri absoluteUrl,
      string? title,
      WebPageFetchErrorKind? fetchErrorKind,
      string fetchErrorMessage,
      string? fetcher = null,
      string? browserStrategy = null
   )
   {
      var absoluteUrlString = absoluteUrl.ToString();

      return new WebPageContent(
         string.IsNullOrWhiteSpace(title) ? absoluteUrlString : title,
         absoluteUrlString,
         null,
         [],
         string.Empty,
         false,
         string.Empty,
         fetchErrorMessage,
         fetchErrorKind,
         fetcher,
         browserStrategy
      );
   }

   internal static string? GetCountryDisplayName(string? countryCode)
   {
      if(string.IsNullOrWhiteSpace(countryCode))
      {
         return null;
      }

      var normalizedCode = countryCode.Trim().ToUpperInvariant();

      if(IsPrimaryCountryCode(normalizedCode))
      {
         return PrimaryCountry.CountryName;
      }

      if(normalizedCode.Length == 3 &&
         CountryNamesByThreeLetterCode is
         { } threeLetterCountryNames &&
         threeLetterCountryNames.TryGetValue(
            normalizedCode,
            out var threeLetterDisplayName
         ))
      {
         return threeLetterDisplayName;
      }

      try
      {
         return new RegionInfo(normalizedCode)
            .EnglishName;
      }
      catch(ArgumentException)
      {
         return null;
      }
   }

   private static string CollapseAdjacentCountryNameDuplicates(string text)
   {
      return RepeatedCountryNameRegex.Value.Replace(
         text,
         match => match.Groups["country"].Value
      );
   }

   private static bool IsStandaloneNoiseLine(string line)
   {
      if(string.IsNullOrWhiteSpace(line))
      {
         return true;
      }

      if(StandaloneNoiseLineRegex.IsMatch(line))
      {
         return true;
      }

      if(line.Length <= 3 &&
         line.All(char.IsLetter) &&
         line.All(char.IsLower))
      {
         return true;
      }

      return false;
   }

   private static IReadOnlyList<string> CollapseAdjacentNameFragmentRuns(
      IEnumerable<string> lines
   )
   {
      var result = new List<string>();
      var bufferedLines = lines.ToList();

      for(var index = 0; index < bufferedLines.Count;)
      {
         var currentLine = bufferedLines[index];

         if(!IsNameFragmentLine(currentLine))
         {
            result.Add(currentLine);
            index++;

            continue;
         }

         var runEnd = index + 1;

         while(runEnd < bufferedLines.Count &&
               IsNameFragmentLine(bufferedLines[runEnd]))
         {
            runEnd++;
         }

         if(runEnd - index > 1)
         {
            result.Add(string.Join(
               " ",
               bufferedLines.Skip(index).Take(runEnd - index)
            ));
            index = runEnd;
            continue;
         }

         result.Add(currentLine);
         index++;
      }

      return result;
   }

   private static bool IsNameFragmentLine(string line)
   {
      if(string.IsNullOrWhiteSpace(line) ||
         line.Any(char.IsDigit) ||
         line.Contains("  ", StringComparison.Ordinal))
      {
         return false;
      }

      var tokens = line.Split(
         ' ',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      );

      if(tokens.Length != 1)
      {
         return false;
      }

      var token = tokens[0];

      if(token.Length < 2 || token.Length > 40)
      {
         return false;
      }

      if(token.All(char.IsUpper))
      {
         return false;
      }

      return char.IsLetter(token[0]);
   }

   internal static string? ExtractHtmlTitle(string html)
   {
      var match = Regex.Match(
         html,
         @"<title[^>]*>(.*?)</title>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );

      if(!match.Success)
      {
         return null;
      }

      return WebUtility.HtmlDecode(
         StripTags(match.Groups[1].Value).Trim()
      );
   }

   internal static DateTimeOffset? ExtractPublishedAt(string html)
   {
      foreach(Match match in HtmlMetaRegex.Matches(html))
      {
         var attributes = match.Groups["attrs"].Value;

         if(!IsPublishedDateMetadata(attributes) ||
            !TryGetAttributeValue(
               attributes,
               "content",
               out var content
            ) ||
            !TryParsePublishedDate(content, out var publishedAt))
         {
            continue;
         }

         return publishedAt;
      }

      foreach(Match match in HtmlScriptRegex.Matches(html))
      {
         var attributes = match.Groups["attrs"].Value;

         if(!IsJsonLdScript(attributes) ||
            !TryExtractPublishedDateFromJson(
               match.Groups["content"].Value,
               out var publishedAt
            ))
         {
            continue;
         }

         return publishedAt;
      }

      foreach(Match match in HtmlTimeRegex.Matches(html))
      {
         var attributes = match.Groups["attrs"].Value;

         if(!TryGetAttributeValue(
               attributes,
               "itemprop",
               out var itemProperty
            ) ||
            !string.Equals(
               itemProperty,
               "datePublished",
               StringComparison.OrdinalIgnoreCase
            ) ||
            !TryGetTimeDateValue(match, out var publishedAt))
         {
            continue;
         }

         return publishedAt;
      }

      foreach(Match match in HtmlTimeRegex.Matches(html))
      {
         if(TryGetTimeDateValue(match, out var publishedAt))
         {
            return publishedAt;
         }
      }

      return null;
   }

   internal static string ExtractHtmlTextWithEmbeddedState(string html)
   {
      var tableText = ExtractStructuredTableText(html);
      var embeddedText = ExtractEmbeddedStateText(html);
      var visibleText = ExtractHtmlText(
         string.IsNullOrWhiteSpace(tableText)
            ? html
            : RemoveNativeTableElements(html)
      );

      return NormalizeText(
         string.Join(
            Environment.NewLine,
            new[]
            {
               tableText,
               embeddedText,
               visibleText
            }.Where(text => !string.IsNullOrWhiteSpace(text))
         )
      );
   }

   internal static IReadOnlyList<string> ExtractHtmlHeadings(string html)
   {
      if(string.IsNullOrWhiteSpace(html))
      {
         return [];
      }

      var sourceHtml = ExtractRelevantLinkSourceHtml(html);
      var headings = new List<string>();

      foreach(Match match in HtmlHeadingRegex.Matches(sourceHtml))
      {
         var headingText = NormalizeText(
            WebUtility.HtmlDecode(
               StripTags(match.Groups["content"].Value)
            )
         );
         headingText = RemoveTemplateArtifacts(headingText);

         if(string.IsNullOrWhiteSpace(headingText) ||
            PlaceholderHeadingRegex.IsMatch(headingText))
         {
            continue;
         }

         var level = match.Groups["level"].Value;
         headings.Add($"H{level}: {headingText}");
      }

      return headings;
   }

   internal static string? DetectIncompleteContentWarning(string? text)
   {
      var normalizedText = NormalizeText(text);

      if(string.IsNullOrWhiteSpace(normalizedText))
      {
         return null;
      }

      var matches = IncompleteContentMarkerRegex.Matches(normalizedText);
      var hasTemplateMarkup = TemplatePlaceholderRegex.IsMatch(
         normalizedText
      ) ||
         TemplateDirectiveRegex.IsMatch(normalizedText) ||
         PlaceholderHeadingLineRegex.IsMatch(normalizedText);
      var warnings = new List<string>();

      if(matches.Count >=
         WebPageFetchDefaults.IncompleteContentMinimumMarkerCount)
      {
         var markers = matches
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

         warnings.Add(
            $"placeholder content was detected ({string.Join(", ", markers)})"
         );
      }

      if(hasTemplateMarkup)
      {
         warnings.Add("template markup was detected");
      }

      if(warnings.Count == 0)
      {
         return null;
      }

      return "Rendered page may be incomplete; " +
         $"{string.Join("; ", warnings)}.";
   }

   internal static string RemoveTemplateArtifacts(string text)
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         return string.Empty;
      }

      var cleanedText = TemplatePlaceholderRegex.Replace(text, " ");
      cleanedText = TemplateDirectiveRegex.Replace(cleanedText, " ");
      cleanedText = PlaceholderHeadingLineRegex.Replace(cleanedText, " ");
      return NormalizeText(cleanedText);
   }

   private static bool IsPublishedDateMetadata(string attributes)
   {
      foreach(var attributeName in new[]
      {
         "property",
         "name",
         "itemprop"
      })
      {
         if(!TryGetAttributeValue(
               attributes,
               attributeName,
               out var value
            ))
         {
            continue;
         }

         var normalizedValue = value
            .Replace(":", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

         if(normalizedValue is
            "articlepublishedtime" or
            "publishedtime" or
            "datepublished" or
            "publishdate")
         {
            return true;
         }
      }

      return false;
   }

   private static bool IsJsonLdScript(string attributes)
   {
      return TryGetAttributeValue(
            attributes,
            "type",
            out var type
         ) &&
         string.Equals(
            type,
            "application/ld+json",
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static bool TryExtractPublishedDateFromJson(
      string content,
      out DateTimeOffset publishedAt
   )
   {
      publishedAt = default;

      try
      {
         using var document = JsonDocument.Parse(
            WebUtility.HtmlDecode(content).Trim()
         );
         var result = FindPublishedDate(document.RootElement);

         if(result is not DateTimeOffset value)
         {
            return false;
         }

         publishedAt = value;
         return true;
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static DateTimeOffset? FindPublishedDate(JsonElement element)
   {
      switch(element.ValueKind)
      {
         case JsonValueKind.Object:
            foreach(var property in element.EnumerateObject())
            {
               if(string.Equals(
                     property.Name,
                     "datePublished",
                     StringComparison.OrdinalIgnoreCase
                  ) &&
                  property.Value.ValueKind == JsonValueKind.String &&
                  TryParsePublishedDate(
                     property.Value.GetString(),
                     out var publishedAt
                  ))
               {
                  return publishedAt;
               }
            }

            foreach(var property in element.EnumerateObject())
            {
               var nestedDate = FindPublishedDate(property.Value);

               if(nestedDate is not null)
               {
                  return nestedDate;
               }
            }

            break;
         case JsonValueKind.Array:
            foreach(var item in element.EnumerateArray())
            {
               var nestedDate = FindPublishedDate(item);

               if(nestedDate is not null)
               {
                  return nestedDate;
               }
            }

            break;
      }

      return null;
   }

   private static bool TryGetTimeDateValue(
      Match match,
      out DateTimeOffset publishedAt
   )
   {
      var attributes = match.Groups["attrs"].Value;
      var value = TryGetAttributeValue(
         attributes,
         "datetime",
         out var datetime
      )
         ? datetime
         : WebUtility.HtmlDecode(
            StripTags(match.Groups["content"].Value)
         ).Trim();

      return TryParsePublishedDate(value, out publishedAt);
   }

   private static bool TryParsePublishedDate(
      string? value,
      out DateTimeOffset publishedAt
   )
   {
      return DateTimeOffset.TryParse(
         value,
         CultureInfo.InvariantCulture,
         DateTimeStyles.AllowWhiteSpaces |
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal,
         out publishedAt
      );
   }

   internal static IReadOnlyList<WebPageRelevantLink>
      ExtractRelevantLinksFromHtml(string html, Uri absoluteUrl)
   {
      if(string.IsNullOrWhiteSpace(html))
      {
         return [];
      }

      var candidateHtml = ExtractRelevantLinkSourceHtml(html);
      if(string.IsNullOrWhiteSpace(candidateHtml))
      {
         return [];
      }

      var scoredLinks = new List<(WebPageRelevantLink Link, int Score)>();
      var seenLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach(Match match in HtmlAnchorRegex.Matches(candidateHtml))
      {
         if(!TryGetAttributeValue(
            match.Groups["attrs"].Value,
            "href",
            out var href
         ))
         {
            continue;
         }

         if(!TryBuildRelevantLinkUrl(absoluteUrl, href, out var linkUrl))
         {
            continue;
         }

         var linkLabel = ExtractRelevantLinkLabel(
            match.Groups["content"].Value,
            linkUrl
         );

         if(!ShouldCaptureRelevantLink(linkLabel, linkUrl))
         {
            continue;
         }

         AddRelevantLink(
            linkLabel,
            linkUrl,
            scoredLinks,
            seenLinks
         );
      }

      AddPdfHrefLinks(
         candidateHtml,
         absoluteUrl,
         scoredLinks,
         seenLinks
      );

      return scoredLinks
         .OrderByDescending(link => link.Score)
         .Select(link => link.Link)
         .Take(WebPageFetchDefaults.MaxRelevantLinkCount)
         .ToArray();
   }

   internal static IReadOnlyList<WebPageRelevantLink> MergeRelevantLinks(
      params IReadOnlyList<WebPageRelevantLink>?[] linkSets
   )
   {
      var mergedLinks = new List<(WebPageRelevantLink Link, int Index)>();
      var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach(var linkSet in linkSets)
      {
         if(linkSet is null)
         {
            continue;
         }

         foreach(var link in linkSet)
         {
            if(string.IsNullOrWhiteSpace(link.Url) ||
               !seenUrls.Add(link.Url.Trim()))
            {
               continue;
            }

            mergedLinks.Add((link, mergedLinks.Count));
         }
      }

      return mergedLinks
         .OrderByDescending(link => IsPdfUrl(link.Link.Url))
         .ThenBy(link => link.Index)
         .Select(link => link.Link)
         .Take(WebPageFetchDefaults.MaxRelevantLinkCount)
         .ToArray();
   }

   internal static IReadOnlyList<WebPageImageCandidate>
      ExtractRelevantImagesFromHtml(
         string html,
         Uri absoluteUrl
      )
   {
      var sourceHtml = ExtractRelevantLinkSourceHtml(html);
      var candidates = new List<(WebPageImageCandidate Image, int Score)>();
      var seenUrls = new HashSet<string>(
         StringComparer.OrdinalIgnoreCase
      );

      foreach(Match match in HtmlImageRegex.Matches(sourceHtml))
      {
         var attributes = match.Groups["attrs"].Value;

         if(!TryGetAttributeValue(attributes, "src", out var source) ||
            !Uri.TryCreate(absoluteUrl, source, out var imageUri) ||
            imageUri.Scheme is not ("http" or "https") ||
            !seenUrls.Add(imageUri.AbsoluteUri))
         {
            continue;
         }

         TryGetAttributeValue(attributes, "alt", out var alt);
         TryGetAttributeValue(attributes, "class", out var cssClass);
         var width = GetPositiveAttributeInt(attributes, "width");
         var height = GetPositiveAttributeInt(attributes, "height");
         var semanticText =
            $"{source} {alt} {cssClass}";
         var semanticMatch = IsDocumentImageText(semanticText);
         var isLargeImage =
            width >= WebPageFetchDefaults.ImageOcrMinimumWidth &&
            height >= WebPageFetchDefaults.ImageOcrMinimumHeight &&
            width * height >= WebPageFetchDefaults.ImageOcrMinimumArea;

         if(!semanticMatch && !isLargeImage)
         {
            continue;
         }

         candidates.Add((
            new WebPageImageCandidate(
               imageUri.AbsoluteUri,
               width,
               height,
               string.IsNullOrWhiteSpace(alt) ? null : alt
            ),
            semanticMatch ? 1 : 0
         ));
      }

      return candidates
         .OrderByDescending(candidate => candidate.Score)
         .ThenByDescending(
            candidate => candidate.Image.Width *
               candidate.Image.Height
         )
         .Take(WebPageFetchDefaults.ImageOcrMaximumCandidateCount)
         .Select(candidate => candidate.Image)
         .ToArray();
   }

   internal static string ExtractHtmlText(string html)
   {
      var cleanedHtml = RemoveBoilerplateHtml(html);
      cleanedHtml = ReplaceFlagsWithCountryLabels(cleanedHtml);
      cleanedHtml = HtmlLineBreakTagRegex.Replace(
         cleanedHtml,
         Environment.NewLine
      );
      cleanedHtml = Regex.Replace(
         cleanedHtml,
         @"<[^>]+>",
         " "
      );

      return NormalizeText(WebUtility.HtmlDecode(cleanedHtml));
   }

   private static string ReplaceFlagsWithCountryLabels(string html)
   {
      var normalizedHtml = HtmlSvgRegex.Replace(
         html,
         match =>
         {
            var label = GetFlagSvgCountryLabel(
               match.Groups["attrs"].Value,
               match.Groups["content"].Value
            );

            return label is null ? match.Value : $" {label} ";
         }
      );
      normalizedHtml = HtmlEmptyFlagElementRegex.Replace(
         normalizedHtml,
         match =>
         {
            var label = GetFlagImageCountryLabel(
               match.Groups["attrs"].Value
            );

            return label is null ? match.Value : $" {label} ";
         }
      );

      return HtmlImageRegex.Replace(
         normalizedHtml,
         match =>
         {
            var label = GetFlagImageCountryLabel(
               match.Groups["attrs"].Value
            );

            return label is null ? " " : $" {label} ";
         }
      );
   }

   private static string? GetFlagSvgCountryLabel(
      string attributes,
      string content
   )
   {
      var label = GetFlagImageCountryLabel(attributes);

      if(label is not null)
      {
         return label;
      }

      foreach(Match match in HtmlUseRegex.Matches(content))
      {
         foreach(var attributeName in new[]
         {
            "href",
            "xlink:href"
         })
         {
            if(!TryGetAttributeValue(
               match.Groups["attrs"].Value,
               attributeName,
               out var attributeValue
            ))
            {
               continue;
            }

            var sourceLabel = GetFlagLabelFromAttribute(
               "src",
               attributeValue
            );

            if(sourceLabel is not null)
            {
               return ResolveCountryFlagLabel(sourceLabel);
            }
         }
      }

      return null;
   }

   private static string? GetFlagImageCountryLabel(string attributes)
   {
      if(!IsFlagImageCandidate(attributes))
      {
         return null;
      }

      foreach(var attributeName in new[]
      {
         "alt",
         "title",
         "aria-label"
      })
      {
         if(!TryGetAttributeValue(
            attributes,
            attributeName,
            out var attributeValue
         ))
         {
            continue;
         }

         var label = NormalizeFlagLabel(attributeValue);

         if(label is not null)
         {
            return ResolveCountryFlagLabel(label);
         }
      }

      foreach(var attributeName in new[]
      {
         "src",
         "srcset",
         "class",
         "data-class"
      })
      {
         if(!TryGetAttributeValue(
            attributes,
            attributeName,
            out var attributeValue
         ))
         {
            continue;
         }

         var label = GetFlagLabelFromAttribute(attributeName, attributeValue);

         if(label is not null)
         {
            return ResolveCountryFlagLabel(label);
         }
      }

      return null;
   }

   private static bool IsFlagImageCandidate(string attributes)
   {
      foreach(var attributeName in new[]
      {
         "alt",
         "title",
         "aria-label"
      })
      {
         if(!TryGetAttributeValue(
            attributes,
            attributeName,
            out var attributeValue
         ))
         {
            continue;
         }

         if(FlagNoisePrefixRegex.IsMatch(attributeValue) ||
            FlagNoiseSuffixRegex.IsMatch(attributeValue))
         {
            return true;
         }
      }

      foreach(var attributeName in new[]
      {
         "src",
         "srcset"
      })
      {
         if(TryGetAttributeValue(
            attributes,
            attributeName,
            out var attributeValue
         ) &&
            (FlagSourceCodeRegex.IsMatch(attributeValue) ||
               FlagNamedCountryRegex.IsMatch(attributeValue)))
         {
            return true;
         }
      }

      foreach(var attributeName in new[]
      {
         "class",
         "data-class"
      })
      {
         if(TryGetAttributeValue(
            attributes,
            attributeName,
            out var attributeValue
         ) &&
            attributeValue.Contains("flag", StringComparison.OrdinalIgnoreCase))
         {
            return true;
         }
      }

      return false;
   }

   private static string? GetFlagLabelFromAttribute(
      string attributeName,
      string attributeValue
   )
   {
      if(string.Equals(
         attributeName,
         "class",
         StringComparison.OrdinalIgnoreCase
      ) ||
         string.Equals(
            attributeName,
            "data-class",
            StringComparison.OrdinalIgnoreCase
         ))
      {
         var classMatch = FlagClassCodeRegex.Match(attributeValue);

         if(classMatch.Success)
         {
            return classMatch.Groups["code"].Value;
         }

         var classTokens = attributeValue.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
               StringSplitOptions.TrimEntries
         );
         var flagTokenIndex = Array.FindIndex(
            classTokens,
            token => string.Equals(
               token,
               "flag",
               StringComparison.OrdinalIgnoreCase
            )
         );

         if(flagTokenIndex < 0)
         {
            return null;
         }

         return classTokens
            .Skip(flagTokenIndex + 1)
            .FirstOrDefault(token =>
               token.Length is 2 or 3 &&
               token.All(char.IsLetter)
            );
      }

      foreach(var sourceCandidate in attributeValue.Split(
         ',',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      ))
      {
         var urlCandidate = sourceCandidate.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries
         ).FirstOrDefault();

         if(string.IsNullOrWhiteSpace(urlCandidate))
         {
            continue;
         }

         var namedCountryMatch = FlagNamedCountryRegex.Match(urlCandidate);

         if(namedCountryMatch.Success)
         {
            return namedCountryMatch.Groups["country"].Value.Replace(
               '_',
               ' '
            );
         }

         var sourceMatch = FlagSourceCodeRegex.Match(urlCandidate);

         if(sourceMatch.Success)
         {
            return sourceMatch.Groups["code"].Value;
         }
      }

      return null;
   }

   private static string? NormalizeFlagLabel(string label)
   {
      var normalizedLabel = NormalizeText(label);

      if(string.IsNullOrWhiteSpace(normalizedLabel))
      {
         return null;
      }

      normalizedLabel = FlagNoisePrefixRegex
         .Replace(normalizedLabel, string.Empty);
      normalizedLabel = FlagNoiseSuffixRegex
         .Replace(normalizedLabel, string.Empty)
         .Trim();

      if(string.IsNullOrWhiteSpace(normalizedLabel))
      {
         return null;
      }

      return normalizedLabel.ToLowerInvariant() is
         "of" or "icon" or "image" or "symbol"
            ? null
            : normalizedLabel;
   }

   private static string ResolveCountryFlagLabel(string label)
   {
      if(IsPrimaryCountryCode(label))
      {
         return PrimaryCountry.CountryName;
      }

      return CountryNamesByCode.TryGetValue(label, out var displayName)
         ? displayName
         : label;
   }

   private static bool IsPrimaryCountryCode(string value)
   {
      value = value.Trim();

      return string.Equals(
         value,
         PrimaryCountry.TwoLetterCode,
         StringComparison.OrdinalIgnoreCase
      ) || string.Equals(
         value,
         PrimaryCountry.ThreeLetterCode,
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static string ExtractRelevantLinkSourceHtml(string html)
   {
      var mainMatch = HtmlMainBlockRegex.Match(html);

      if(mainMatch.Success)
      {
         return RemoveBoilerplateHtml(mainMatch.Groups["content"].Value);
      }

      var bodyMatch = HtmlBodyBlockRegex.Match(html);

      if(bodyMatch.Success)
      {
         return RemoveBoilerplateHtml(bodyMatch.Groups["content"].Value);
      }

      return RemoveBoilerplateHtml(html);
   }

   private static string RemoveBoilerplateHtml(string html)
   {
      var withoutBoilerplate = HtmlBoilerplateBlockRegex.Replace(html, " ");
      withoutBoilerplate = HtmlSelectBlockRegex.Replace(
         withoutBoilerplate,
         " "
      );
      return RemoveAttributedBoilerplateBlocks(withoutBoilerplate);
   }

   private static string RemoveAttributedBoilerplateBlocks(string html)
   {
      var builder = new StringBuilder();
      var lastIndex = 0;
      var skippedDepth = 0;

      foreach(Match match in HtmlTagRegex.Matches(html))
      {
         var isClosingTag = match.Value.StartsWith(
            "</",
            StringComparison.Ordinal
         );
         var isVoidTag = IsVoidHtmlElement(
            match.Groups["name"].Value,
            match.Groups["attrs"].Value
         );

         if(skippedDepth == 0)
         {
            builder.Append(html[lastIndex..match.Index]);

            if(!isClosingTag &&
               !isVoidTag &&
               HasBoilerplateAttributes(match.Groups["attrs"].Value))
            {
               skippedDepth = 1;
            }
            else
            {
               builder.Append(match.Value);
            }
         }
         else if(!isClosingTag && !isVoidTag)
         {
            skippedDepth++;
         }
         else if(isClosingTag)
         {
            skippedDepth--;
         }

         lastIndex = match.Index + match.Length;
      }

      if(skippedDepth == 0)
      {
         builder.Append(html[lastIndex..]);
      }

      return builder.ToString();
   }

   private static bool HasBoilerplateAttributes(string attributes)
   {
      if(TryGetAttributeValue(
            attributes,
            "role",
            out var role
         ) &&
         string.Equals(
            role,
            "dialog",
            StringComparison.OrdinalIgnoreCase
         ))
      {
         return true;
      }

      if(TryGetAttributeValue(
            attributes,
            "aria-modal",
            out var ariaModal
         ) &&
         string.Equals(
            ariaModal,
            "true",
            StringComparison.OrdinalIgnoreCase
         ))
      {
         return true;
      }

      if(TryGetAttributeValue(
            attributes,
            "data-nosnippet",
            out var noSnippet
         ) &&
         string.Equals(
            noSnippet,
            "true",
            StringComparison.OrdinalIgnoreCase
         ))
      {
         return true;
      }

      foreach(var attributeName in new[]
      {
         "id",
         "class"
      })
      {
         if(!TryGetAttributeValue(
               attributes,
               attributeName,
               out var value
            ))
         {
            continue;
         }

         var normalizedValue = value.ToLowerInvariant();

         if(normalizedValue.Contains("cookie", StringComparison.Ordinal) ||
            normalizedValue.Contains("consent", StringComparison.Ordinal) ||
            normalizedValue.Contains("privacy", StringComparison.Ordinal) ||
            normalizedValue.Contains("modal", StringComparison.Ordinal) ||
            normalizedValue.Contains("overlay", StringComparison.Ordinal))
         {
            return true;
         }
      }

      return false;
   }

   private static bool IsVoidHtmlElement(string name, string attributes)
   {
      if(attributes.TrimEnd().EndsWith(
            "/",
            StringComparison.Ordinal
         ))
      {
         return true;
      }

      return name.ToLowerInvariant() is
         "area" or
         "base" or
         "br" or
         "col" or
         "embed" or
         "hr" or
         "img" or
         "input" or
         "link" or
         "meta" or
         "param" or
         "source" or
         "track" or
         "wbr";
   }

   private static bool TryGetAttributeValue(
      string attributes,
      string attributeName,
      out string value
   )
   {
      foreach(Match match in HtmlAttributeRegex.Matches(attributes))
      {
         if(!string.Equals(
            match.Groups["name"].Value,
            attributeName,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            continue;
         }

         value = WebUtility.HtmlDecode(match.Groups["value"].Value.Trim());
         return !string.IsNullOrWhiteSpace(value);
      }

      value = string.Empty;
      return false;
   }

   private static int GetPositiveAttributeInt(
      string attributes,
      string attributeName
   )
   {
      return TryGetAttributeValue(
         attributes,
         attributeName,
         out var value
      ) &&
         int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var result
         ) &&
         result > 0
         ? result
         : 0;
   }

   private static bool IsDocumentImageText(string value)
   {
      return new[]
      {
         "entry",
         "start",
         "result",
         "driver",
         "participant",
         "document",
         "list"
      }.Any(term => value.Contains(
         term,
         StringComparison.OrdinalIgnoreCase
      ));
   }

   private static bool TryBuildRelevantLinkUrl(
      Uri absoluteUrl,
      string href,
      out string linkUrl
   )
   {
      linkUrl = string.Empty;

      if(string.IsNullOrWhiteSpace(href))
      {
         return false;
      }

      if(!Uri.TryCreate(absoluteUrl, href.Trim(), out var linkUri))
      {
         return false;
      }

      if(!string.Equals(
         linkUri.Scheme,
         Uri.UriSchemeHttp,
         StringComparison.OrdinalIgnoreCase
      ) &&
         !string.Equals(
            linkUri.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase
         ))
      {
         return false;
      }

      var strippedFragmentUri = new UriBuilder(linkUri)
      {
         Fragment = string.Empty
      }.Uri;

      if(Uri.Compare(
         strippedFragmentUri,
         absoluteUrl,
         UriComponents.AbsoluteUri,
         UriFormat.UriEscaped,
         StringComparison.OrdinalIgnoreCase
      ) == 0)
      {
         return false;
      }

      linkUrl = strippedFragmentUri.AbsoluteUri;
      return true;
   }

   private static bool ShouldCaptureRelevantLink(
      string label,
      string url
   )
   {
      var isPdfLink = IsPdfUrl(url);

      if(string.IsNullOrWhiteSpace(label))
      {
         return false;
      }

      if(label.Length < 2 || label.Length > 100)
      {
         return false;
      }

      if(!label.Any(char.IsLetter) ||
         WebPageStructuredTextSupport.IsLikelyMachineValue(label))
      {
         return false;
      }

      if(GenericLinkTextRegex.IsMatch(label) && !isPdfLink)
      {
         return false;
      }

      return isPdfLink || IsParticipationListLink(label, url);
   }

   private static void AddPdfHrefLinks(
      string html,
      Uri absoluteUrl,
      List<(WebPageRelevantLink Link, int Score)> scoredLinks,
      HashSet<string> seenLinks
   )
   {
      foreach(Match match in HtmlHrefRegex.Matches(html))
      {
         var href = WebUtility.HtmlDecode(
            match.Groups["value"].Value.Trim()
         );

         if(!TryBuildRelevantLinkUrl(absoluteUrl, href, out var linkUrl) ||
            !IsPdfUrl(linkUrl))
         {
            continue;
         }

         var linkLabel = ExtractRelevantLinkLabel(string.Empty, linkUrl);

         if(!ShouldCaptureRelevantLink(linkLabel, linkUrl))
         {
            continue;
         }

         AddRelevantLink(
            linkLabel,
            linkUrl,
            scoredLinks,
            seenLinks
         );
      }
   }

   private static void AddRelevantLink(
      string linkLabel,
      string linkUrl,
      List<(WebPageRelevantLink Link, int Score)> scoredLinks,
      HashSet<string> seenLinks
   )
   {
      if(!seenLinks.Add(linkUrl.Trim()))
      {
         return;
      }

      scoredLinks.Add((
         new WebPageRelevantLink(linkLabel, linkUrl),
         ScoreRelevantLink(linkLabel, linkUrl)
      ));
   }

   private static string ExtractRelevantLinkLabel(
      string htmlContent,
      string linkUrl
   )
   {
      var label = NormalizeText(
         WebUtility.HtmlDecode(StripTags(htmlContent))
      );

      if(!string.IsNullOrWhiteSpace(label))
      {
         return label;
      }

      if(!IsPdfUrl(linkUrl) ||
         !Uri.TryCreate(linkUrl, UriKind.Absolute, out var uri))
      {
         return label;
      }

      var fileName = Path.GetFileNameWithoutExtension(uri.AbsolutePath);

      return NormalizeText(
         WebUtility.UrlDecode(fileName).Replace('-', ' ')
      );
   }

   private static bool IsParticipationListLink(string label, string url)
   {
      var value = $"{label} {url}".Trim();

      return value.Contains("entry list", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("start list", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("entrylist", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("startlist", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("competitors", StringComparison.OrdinalIgnoreCase) ||
         value.Contains(
            "competitor list",
            StringComparison.OrdinalIgnoreCase
         ) ||
         value.Contains("players", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("player list", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("lineup", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("line-up", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("entries", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("participants", StringComparison.OrdinalIgnoreCase) ||
         value.Contains(
            "participant list",
            StringComparison.OrdinalIgnoreCase
         ) ||
         value.Contains("roster", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("grid", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("drivers", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("driver list", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("riders", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("rider list", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("trupp", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("squad", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("deltagarlista", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("startlista", StringComparison.OrdinalIgnoreCase);
   }

   private static int ScoreRelevantLink(
      string label,
      string url
   )
   {
      var score = 0;

      if(IsPdfUrl(url))
      {
         score += 6;
      }

      if(RelevantLinkLabelBoostRegex.IsMatch(label))
      {
         score += 4;
      }

      if(label.Contains("entry list", StringComparison.OrdinalIgnoreCase) ||
         label.Contains("start list", StringComparison.OrdinalIgnoreCase))
      {
         score += 4;
      }

      if(label.Length <= 20)
      {
         score += 1;
      }

      return score;
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

   internal static string ExtractEmbeddedStateText(string html)
   {
      return EmbeddedStateTextExtractor.ExtractText(html);
   }

   internal static string ExtractStructuredTableText(string html)
   {
      var nativeTexts = new List<string>();
      var preferredTexts = new List<string>();
      var otherTexts = new List<string>();
      var seenTexts = new HashSet<string>(StringComparer.Ordinal);

      ExtractNativeTableRows(
         html,
         nativeTexts,
         seenTexts
      );

      var tableMatches = Regex.Matches(
         html,
         @"<(?<tag>[a-zA-Z0-9:-]+)(?<attrs>[^>]*)\brole=""" +
         @"(?<role>cell|gridcell|rowheader|columnheader)""[^>]*>" +
         @"(?<content>.*?)</\k<tag>>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );

      foreach(Match match in tableMatches)
      {
         var normalizedText = NormalizeText(
            WebUtility.HtmlDecode(
               StripTags(match.Groups["content"].Value)
            )
         );

         if(!WebPageStructuredTextSupport.ShouldCaptureEmbeddedValue(
               null,
               normalizedText
            ) ||
            !seenTexts.Add(normalizedText))
         {
            continue;
         }

         if(WebPageStructuredTextSupport.IsLikelyReadableStructuredPhrase(
               normalizedText
            ))
         {
            preferredTexts.Add(normalizedText);
         }
         else
         {
            otherTexts.Add(normalizedText);
         }
      }

      var roleTexts = preferredTexts.Count > 0
         ? preferredTexts
         : otherTexts;
      var texts = nativeTexts.Concat(roleTexts);
      return NormalizeText(string.Join(Environment.NewLine, texts));
   }

   private static void ExtractNativeTableRows(
      string html,
      ICollection<string> texts,
      ISet<string> seenTexts
   )
   {
      var tableMatches = Regex.Matches(
         html,
         @"<table\b[^>]*>(?<content>.*?)</table>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );

      foreach(Match tableMatch in tableMatches)
      {
         var rowMatches = Regex.Matches(
            tableMatch.Groups["content"].Value,
            @"<tr\b[^>]*>(?<content>.*?)</tr>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
         );

         foreach(Match rowMatch in rowMatches)
         {
            var cells = ExtractNativeTableCells(
               rowMatch.Groups["content"].Value
            );

            if(cells.Count == 0)
            {
               continue;
            }

            var rowText = string.Join(" | ", cells);

            if(seenTexts.Add(rowText))
            {
               texts.Add(rowText);
            }
         }
      }
   }

   private static IReadOnlyList<string> ExtractNativeTableCells(
      string rowHtml
   )
   {
      var cells = new List<string>();
      var cellMatches = Regex.Matches(
         rowHtml,
         @"<(?:th|td)\b[^>]*>(?<content>.*?)</(?:th|td)>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );

      foreach(Match cellMatch in cellMatches)
      {
         var cellHtml = ReplaceFlagsWithCountryLabels(
            cellMatch.Groups["content"].Value
         );
         var cellText = NormalizeText(
            WebUtility.HtmlDecode(StripTags(cellHtml))
         );

         if(!string.IsNullOrWhiteSpace(cellText))
         {
            cells.Add(cellText);
         }
      }

      return cells;
   }

   private static string RemoveNativeTableElements(string html)
   {
      return Regex.Replace(
         html,
         @"<table\b[^>]*>.*?</table>",
         " ",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );
   }

   internal static string ExtractPdfText(PdfDocument pdfDocument)
   {
      return PdfContentExtractor.ExtractText(pdfDocument);
   }

   internal static string ExtractPdfTitle(
      PdfDocument pdfDocument,
      Uri absoluteUrl
   )
   {
      return PdfContentExtractor.ExtractTitle(pdfDocument, absoluteUrl);
   }

   private static string StripTags(string value)
   {
      return Regex.Replace(value, @"<[^>]+>", " ");
   }

   private static IReadOnlyDictionary<string, string> BuildCountryNamesByCode()
   {
      var countryNames = new Dictionary<string, string>(
         StringComparer.OrdinalIgnoreCase
      );

      foreach(var culture in CultureInfo.GetCultures(
         CultureTypes.SpecificCultures
      ))
      {
         RegionInfo? region;

         try
         {
            region = new RegionInfo(culture.Name);
         }
         catch(ArgumentException)
         {
            continue;
         }

         var code = region.TwoLetterISORegionName;

         if(countryNames.ContainsKey(code))
         {
            continue;
         }

         countryNames[code] = region.EnglishName;
         countryNames[region.ThreeLetterISORegionName] =
            region.EnglishName;
      }

      countryNames[PrimaryCountry.TwoLetterCode] =
         PrimaryCountry.CountryName;
      countryNames[PrimaryCountry.ThreeLetterCode] =
         PrimaryCountry.CountryName;

      return countryNames;
   }

   private static IReadOnlyDictionary<string, string>
      BuildCountryNamesByThreeLetterCode()
   {
      var countryNames = new Dictionary<string, string>(
         StringComparer.OrdinalIgnoreCase
      );

      foreach(var culture in CultureInfo.GetCultures(
         CultureTypes.SpecificCultures
      ))
      {
         RegionInfo? region;

         try
         {
            region = new RegionInfo(culture.Name);
         }
         catch(ArgumentException)
         {
            continue;
         }

         var code = region.ThreeLetterISORegionName;

         if(countryNames.ContainsKey(code))
         {
            continue;
         }

         countryNames[code] = region.EnglishName;
      }

      countryNames[PrimaryCountry.ThreeLetterCode] =
         PrimaryCountry.CountryName;

      return countryNames;
   }

   private static string BuildSecChUaHeader(string browserUserAgent)
   {
      var majorVersionMatch = Regex.Match(
         browserUserAgent,
         @"Chrome/(\d+)",
         RegexOptions.CultureInvariant
      );

      var majorVersion = majorVersionMatch.Success &&
         int.TryParse(
            majorVersionMatch.Groups[1].Value,
            out var parsedMajorVersion
         )
         ? parsedMajorVersion
         : WebPageFetchDefaults.BrowserUserAgentFallbackMajorVersion;

      return
         $"\"Chromium\";v=\"{majorVersion}\", " +
         $"\"Not A(Brand\";v=\"24\", \"Google Chrome\";v=\"{majorVersion}\"";
   }

   private static Regex BuildRepeatedCountryNameRegex()
   {
      var countryNames = CountryNamesByCode.Values
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .OrderByDescending(name => name.Length)
         .Select(Regex.Escape)
         .ToArray();

      var pattern =
         $@"\b(?<country>{string.Join("|", countryNames)})\b" +
         @"(?:(?:\s+\|\s+|\s+)\k<country>\b)+";

      return new Regex(
         pattern,
         RegexOptions.CultureInvariant | RegexOptions.IgnoreCase |
            RegexOptions.Compiled
      );
   }

   private static readonly Lazy<Regex> RepeatedCountryNameRegex =
      new(BuildRepeatedCountryNameRegex);

   private static async Task<string> BuildBrowserUserAgentAsync()
   {
      try
      {
         using var playwright = await Playwright.CreateAsync();
         await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
               Headless = true
            }
         );

         return BuildBrowserUserAgent(browser.Version);
      }
      catch
      {
         return WebPageFetchDefaults.BrowserUserAgentFallback;
      }
   }
}
