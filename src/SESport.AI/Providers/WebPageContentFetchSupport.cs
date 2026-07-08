using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace SESport.AI.Providers;

internal static class WebPageContentFetchSupport
{
   private const string BrowserUserAgentFallback =
      "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
      "(KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36";
   private const string BrowserAcceptHeader =
      "text/html,application/xhtml+xml,application/xml;q=0.9," +
      "image/avif,image/webp,*/*;q=0.8";
   private const string BrowserAcceptLanguageHeader = "en-US,en;q=0.9";
   private const string BrowserLocale = "en-US";
   private const string BrowserPlatform = "Linux";
   private const string BrowserFingerprintScript = """
      Object.defineProperty(navigator, 'webdriver', {
         get: () => undefined
      });
      Object.defineProperty(navigator, 'languages', {
         get: () => ['en-US', 'en']
      });
      Object.defineProperty(navigator, 'platform', {
         get: () => 'Linux x86_64'
      });
      Object.defineProperty(navigator, 'vendor', {
         get: () => 'Google Inc.'
      });
      """;

   internal const string CutoffMarker = "[CUTOFF]";
   internal const int MaxResponseCharacters = 50000;

   internal static readonly TimeSpan BrowserNavigationTimeout =
      TimeSpan.FromSeconds(30);
   internal static readonly TimeSpan BrowserLoadStateTimeout =
      TimeSpan.FromSeconds(30);
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
   private static readonly Regex HtmlAttributeRegex = new(
      @"\b(?<name>[a-zA-Z0-9:-]+)\s*=\s*(?:" +
      @"""(?<value>[^""]*)""|'(?<value>[^']*)')",
      RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
   );
   private static readonly Regex HtmlBoilerplateBlockRegex = new(
      @"<(?:header|nav|footer|aside|script|style|noscript)\b[^>]*>.*?</" +
      @"(?:header|nav|footer|aside|script|style|noscript)>",
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
   private static readonly Regex GluedGolfClubRegex = new(
      @"(?<=[\p{Ll}])(?=[\p{Lu}][\p{L}'’&.\- ]*" +
      @"\s+(?:GC|G&CC|G&SC|GK|CC|Club|Links|Estate|Resort)\b)",
      RegexOptions.CultureInvariant | RegexOptions.Compiled
   );

   internal static async Task<string> GetBrowserUserAgentAsync()
   {
      try
      {
         return await BrowserUserAgentTask.Value;
      }
      catch
      {
         return BrowserUserAgentFallback;
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
         return BrowserUserAgentFallback;
      }

      return
         "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
         $"(KHTML, like Gecko) Chrome/{majorVersion}.0.0.0 Safari/537.36";
   }

   internal static IReadOnlyDictionary<string, string>
      BuildBrowserLikeHeaders(string browserUserAgent)
   {
      return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      {
         ["Accept"] = BrowserAcceptHeader,
         ["Accept-Language"] = BrowserAcceptLanguageHeader,
         ["Upgrade-Insecure-Requests"] = "1",
         ["Sec-CH-UA"] = BuildSecChUaHeader(browserUserAgent),
         ["Sec-CH-UA-Mobile"] = "?0",
         ["Sec-CH-UA-Platform"] = $"\"{BrowserPlatform}\""
      };
   }

   internal static string NormalizeText(string? text)
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         return string.Empty;
      }

      text = GluedGolfClubRegex.Replace(text, " | ");

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

   internal static string ApplyResponseCutoff(string text)
   {
      if(string.IsNullOrWhiteSpace(text) ||
         text.Length <= MaxResponseCharacters)
      {
         return text;
      }

      var cutoffLength = MaxResponseCharacters - CutoffMarker.Length;

      if(cutoffLength <= 0)
      {
         return CutoffMarker;
      }

      return text[..cutoffLength].TrimEnd() + CutoffMarker;
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
      string? fetcher = null
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
         fetcher
      );
   }

   internal static string? GetCountryDisplayName(string? countryCode)
   {
      if(string.IsNullOrWhiteSpace(countryCode))
      {
         return null;
      }

      var normalizedCode = countryCode.Trim().ToUpperInvariant();

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

      for(var index = 0; index < bufferedLines.Count; )
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

   internal static string ExtractHtmlTextWithEmbeddedState(string html)
   {
      var tableText = ExtractStructuredTableText(html);
      var embeddedText = ExtractEmbeddedStateText(html);
      var visibleText = ExtractHtmlText(html);

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

         var linkLabel = NormalizeText(
            WebUtility.HtmlDecode(
               StripTags(match.Groups["content"].Value)
            )
         );

         if(!ShouldCaptureRelevantLink(linkLabel, linkUrl))
         {
            continue;
         }

         var linkKey = $"{linkLabel}\n{linkUrl}";

         if(!seenLinks.Add(linkKey))
         {
            continue;
         }

         scoredLinks.Add((
            new WebPageRelevantLink(linkLabel, linkUrl),
            ScoreRelevantLink(linkLabel)
         ));
      }

      return scoredLinks
         .OrderByDescending(link => link.Score)
         .Select(link => link.Link)
         .Take(10)
         .ToArray();
   }

   internal static string ExtractHtmlText(string html)
   {
      var cleanedHtml = RemoveBoilerplateHtml(html);
      cleanedHtml = ReplaceFlagImagesWithCountryLabels(cleanedHtml);
      cleanedHtml = Regex.Replace(
         cleanedHtml,
         @"<[^>]+>",
         " "
      );

      return NormalizeText(WebUtility.HtmlDecode(cleanedHtml));
   }

   private static string ReplaceFlagImagesWithCountryLabels(string html)
   {
      return HtmlImageRegex.Replace(
         html,
         match =>
         {
            var label = GetFlagImageCountryLabel(
               match.Groups["attrs"].Value
            );

            return label is null ? " " : $" {label} ";
         }
      );
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
            FlagSourceCodeRegex.IsMatch(attributeValue))
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

         return classMatch.Success
            ? classMatch.Groups["code"].Value
            : null;
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
      return CountryNamesByCode.TryGetValue(label, out var displayName)
         ? displayName
         : label;
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
      return HtmlBoilerplateBlockRegex.Replace(html, " ");
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

      linkUrl = strippedFragmentUri.ToString();
      return true;
   }

   private static bool ShouldCaptureRelevantLink(
      string label,
      string url
   )
   {
      if(string.IsNullOrWhiteSpace(label))
      {
         return false;
      }

      if(label.Length < 2 || label.Length > 100)
      {
         return false;
      }

      if(!label.Any(char.IsLetter) || IsLikelyMachineValue(label))
      {
         return false;
      }

      if(GenericLinkTextRegex.IsMatch(label))
      {
         return false;
      }

      return IsParticipationListLink(label, url);
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

   private static int ScoreRelevantLink(string label)
   {
      var score = 0;

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

   internal static string ExtractEmbeddedStateText(string html)
   {
      var texts = new List<string>();
      var seenTexts = new HashSet<string>(StringComparer.Ordinal);
      var scriptMatches = Regex.Matches(
         html,
         @"<script\b([^>]*)>(.*?)</script>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline
      );

      foreach(Match match in scriptMatches)
      {
         if(!TryExtractStructuredJsonText(
            match.Groups[1].Value,
            match.Groups[2].Value,
            out var embeddedText
         ))
         {
            continue;
         }

         foreach(var line in embeddedText.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries
         ))
         {
            var normalizedLine = NormalizeText(line);

            if(string.IsNullOrWhiteSpace(normalizedLine) ||
               !seenTexts.Add(normalizedLine))
            {
               continue;
            }

            texts.Add(normalizedLine);
         }
      }

      return NormalizeText(string.Join(Environment.NewLine, texts));
   }

   internal static string ExtractStructuredTableText(string html)
   {
      var preferredTexts = new List<string>();
      var otherTexts = new List<string>();
      var seenTexts = new HashSet<string>(StringComparer.Ordinal);
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

         if(!ShouldCaptureEmbeddedValue(null, normalizedText) ||
            !seenTexts.Add(normalizedText))
         {
            continue;
         }

         if(IsLikelyReadableStructuredPhrase(normalizedText))
         {
            preferredTexts.Add(normalizedText);
         }
         else
         {
            otherTexts.Add(normalizedText);
         }
      }

      var texts = preferredTexts.Count > 0 ? preferredTexts : otherTexts;
      return NormalizeText(string.Join(Environment.NewLine, texts));
   }

   internal static string ExtractPdfText(PdfDocument pdfDocument)
   {
      var pages = pdfDocument
         .GetPages()
         .Select(page => ContentOrderTextExtractor.GetText(page, true))
         .Where(text => !string.IsNullOrWhiteSpace(text))
         .Select(text => text.Trim());

      return string.Join(Environment.NewLine, pages);
   }

   internal static string ExtractPdfTitle(
      PdfDocument pdfDocument,
      Uri absoluteUrl
   )
   {
      var title = pdfDocument.Information.Title?.Trim();

      if(!string.IsNullOrWhiteSpace(title))
      {
         return title;
      }

      var fileName = Path.GetFileNameWithoutExtension(
         absoluteUrl.AbsolutePath
      );

      if(!string.IsNullOrWhiteSpace(fileName))
      {
         return fileName;
      }

      return absoluteUrl.ToString();
   }

   private static bool TryExtractStructuredJsonText(
      string scriptAttributes,
      string scriptContent,
      out string text
   )
   {
      text = string.Empty;
      var normalizedContent = WebUtility.HtmlDecode(scriptContent).Trim();

      if(string.IsNullOrWhiteSpace(normalizedContent))
      {
         return false;
      }

      if(!scriptAttributes.Contains("application/json",
            StringComparison.OrdinalIgnoreCase) &&
         !scriptAttributes.Contains("application/ld+json",
            StringComparison.OrdinalIgnoreCase) &&
         !LooksLikeStructuredScript(normalizedContent))
      {
         return false;
      }

      if(!TryParseJsonDocument(normalizedContent, out var document))
      {
         return false;
      }

      if(document is null)
      {
         return false;
      }

      using(document)
      {
         var values = new List<string>();
         var seenValues = new HashSet<string>(StringComparer.Ordinal);
         CollectEmbeddedText(
            document.RootElement,
            null,
            values,
            seenValues
         );

         text = NormalizeText(string.Join(Environment.NewLine, values));
         return !string.IsNullOrWhiteSpace(text);
      }
   }

   private static bool LooksLikeStructuredScript(string scriptContent)
   {
      if(scriptContent.StartsWith("{", StringComparison.Ordinal) ||
         scriptContent.StartsWith("[", StringComparison.Ordinal) ||
         scriptContent.Contains("__INITIAL_STATE__",
            StringComparison.Ordinal) ||
         scriptContent.Contains("__NEXT_DATA__", StringComparison.Ordinal) ||
         scriptContent.Contains("prerender-data-cache",
            StringComparison.Ordinal))
      {
         return true;
      }

      return scriptContent.Contains("=", StringComparison.Ordinal) &&
         (scriptContent.Contains("{", StringComparison.Ordinal) ||
          scriptContent.Contains("[", StringComparison.Ordinal));
   }

   private static bool TryParseJsonDocument(
      string content,
      out JsonDocument? document
   )
   {
      document = null;

      if(TryParseJsonDocumentCore(content, out document))
      {
         return true;
      }

      if(!TryExtractJsonFragment(content, out var jsonFragment))
      {
         return false;
      }

      return TryParseJsonDocumentCore(jsonFragment, out document);
   }

   private static bool TryParseJsonDocumentCore(
      string content,
      out JsonDocument? document
   )
   {
      document = null;

      try
      {
         document = JsonDocument.Parse(
            content.Trim().TrimEnd(';'),
            new JsonDocumentOptions
            {
               AllowTrailingCommas = true
            }
         );

         if(document.RootElement.ValueKind == JsonValueKind.String)
         {
            var embeddedJson = document.RootElement.GetString();

            if(!string.IsNullOrWhiteSpace(embeddedJson) &&
               TryParseJsonDocumentCore(embeddedJson, out var nestedDocument))
            {
               document.Dispose();
               document = nestedDocument;
            }
         }

         return true;
      }
      catch(JsonException)
      {
         document?.Dispose();
         document = null;
         return false;
      }
   }

   private static bool TryExtractJsonFragment(
      string content,
      out string jsonFragment
   )
   {
      jsonFragment = string.Empty;

      var startIndex = content.IndexOfAny(['{', '[']);

      if(startIndex < 0)
      {
         return false;
      }

      var endIndex = Math.Max(
         content.LastIndexOf('}'),
         content.LastIndexOf(']')
      );

      if(endIndex <= startIndex)
      {
         return false;
      }

      jsonFragment = content[startIndex..(endIndex + 1)];
      return true;
   }

   private static void CollectEmbeddedText(
      JsonElement element,
      string? propertyName,
      ICollection<string> texts,
      ISet<string> seenTexts
   )
   {
      switch(element.ValueKind)
      {
         case JsonValueKind.Object:
            foreach(var property in element.EnumerateObject())
            {
               CollectEmbeddedText(
                  property.Value,
                  property.Name,
                  texts,
                  seenTexts
               );
            }

            break;
         case JsonValueKind.Array:
            foreach(var item in element.EnumerateArray())
            {
               CollectEmbeddedText(item, propertyName, texts, seenTexts);
            }

            break;
         case JsonValueKind.String:
            AddEmbeddedValue(
               propertyName,
               element.GetString(),
               texts,
               seenTexts
            );

            break;
         case JsonValueKind.Number:
         case JsonValueKind.True:
         case JsonValueKind.False:
            AddEmbeddedValue(
               propertyName,
               element.ToString(),
               texts,
               seenTexts
            );

            break;
      }
   }

   private static void AddEmbeddedValue(
      string? propertyName,
      string? value,
      ICollection<string> texts,
      ISet<string> seenTexts
   )
   {
      if(!ShouldCaptureEmbeddedValue(propertyName, value))
      {
         return;
      }

      var normalizedValue = NormalizeText(value);

      if(string.IsNullOrWhiteSpace(normalizedValue) ||
         !seenTexts.Add(normalizedValue))
      {
         return;
      }

      texts.Add(normalizedValue);
   }

   private static bool ShouldCaptureEmbeddedValue(
      string? propertyName,
      string? value
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return false;
      }

      var normalizedValue = NormalizeText(value);

      if(normalizedValue.Length < 2 || normalizedValue.Length > 160)
      {
         return false;
      }

      if(IsLikelyMachineValue(normalizedValue))
      {
         return false;
      }

      if(IsLikelyDisplayProperty(propertyName))
      {
         return true;
      }

      return IsLikelyHumanReadable(normalizedValue);
   }

   private static bool IsLikelyDisplayProperty(string? propertyName)
   {
      if(string.IsNullOrWhiteSpace(propertyName))
      {
         return false;
      }

      var normalizedPropertyName = propertyName.Trim().ToLowerInvariant();

      return normalizedPropertyName.EndsWith(
            "name",
            StringComparison.Ordinal
         ) ||
         normalizedPropertyName.EndsWith("title", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("label", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("text", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("description",
            StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("caption", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith(
            "headline",
            StringComparison.Ordinal
         ) ||
         normalizedPropertyName.EndsWith("standfirst",
            StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("summary", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("alt", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("alttext", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("city", StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("countryname",
            StringComparison.Ordinal) ||
         normalizedPropertyName.EndsWith("displayname",
            StringComparison.Ordinal);
   }

   private static bool IsLikelyMachineValue(string value)
   {
      return value.Contains("://", StringComparison.Ordinal) ||
         value.Contains("/", StringComparison.Ordinal) ||
         value.Contains("rrn:", StringComparison.Ordinal) ||
         value.Contains("urn:", StringComparison.Ordinal) ||
         value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) ||
         value.All(char.IsDigit) ||
         Regex.IsMatch(
            value,
            @"^[0-9a-fA-F]{12,}$",
            RegexOptions.CultureInvariant
         );
   }

   private static bool IsLikelyHumanReadable(string value)
   {
      if(!value.Any(char.IsLetter))
      {
         return false;
      }

      if(value.Contains(" ", StringComparison.Ordinal))
      {
         return true;
      }

      return value.Length <= 5 && value.All(char.IsUpper);
   }

   private static bool IsLikelyReadableStructuredPhrase(string value)
   {
      if(!IsLikelyHumanReadable(value))
      {
         return false;
      }

      var tokens = value.Split(
         ' ',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      );

      if(tokens.Length < 2 || tokens.Length > 4)
      {
         return false;
      }

      if(tokens.Any(IsCommonStructuredLabelToken))
      {
         return false;
      }

      return tokens.All(token =>
         token.All(character =>
            char.IsLetter(character) ||
            character is '-' or '\'' or '’'
         )
      );
   }

   private static bool IsCommonStructuredLabelToken(string token)
   {
      var normalizedToken = token.Trim().ToLowerInvariant();

      return normalizedToken is
         "count" or
         "no." or
         "no" or
         "name" or
         "title" or
         "label" or
         "text" or
         "description" or
         "summary" or
         "status" or
         "type" or
         "category" or
         "class" or
         "group" or
         "rank" or
         "round" or
         "date" or
         "time" or
         "priority" or
         "eligible" or
         "entry" or
         "entries" or
         "item" or
         "items" or
         "value" or
         "values" or
         "country" or
         "city" or
         "table" or
         "row" or
         "column" or
         "cell" or
         "id" or
         "code" or
         "page" or
         "section" or
         "link" or
         "url";
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
         : 125;

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
         return BrowserUserAgentFallback;
      }
   }
}
