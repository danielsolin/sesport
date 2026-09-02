using Microsoft.Playwright;

using SESport.AI.Llama;
using SESport.AI.WebPages;

namespace SESport.Core.Tests.AI;

public class WebPageContentClientTests
{




   [Fact]
   public void ParseImageOcrTsvPreservesVisuallySeparatedColumns()
   {
      var header = string.Join('\t',
      [
         "level",
         "page_num",
         "block_num",
         "par_num",
         "line_num",
         "word_num",
         "left",
         "top",
         "width",
         "height",
         "conf",
         "text"
      ]);
      var rows = """
         5	1	1	1	1	1	140	400	8	12	96.0	1
         5	1	1	1	1	2	199	400	42	12	96.0	Rafael
         5	1	1	1	1	3	246	400	53	12	96.0	Camara
         5	1	1	1	1	4	521	400	27	12	96.0	BRA
         5	1	1	1	1	5	590	400	44	12	96.0	Invicta
         5	1	1	1	1	6	639	400	46	12	96.0	Racing
         5	1	1	1	2	1	140	436	8	12	96.0	2
         5	1	1	1	2	2	199	436	50	12	96.0	Joshua
         5	1	1	1	2	3	254	436	57	12	96.0	Durksen
         5	1	1	1	2	4	521	436	27	12	96.0	PAR
         5	1	1	1	2	5	590	436	44	12	96.0	Invicta
         5	1	1	1	2	6	639	436	46	12	96.0	Racing
         """;
      var tsv = header + Environment.NewLine + rows;

      var text = WebPageImageOcr.ParseTsv(tsv);

      Assert.Equal(
         "1 | Rafael Camara | BRA | Invicta Racing" +
         Environment.NewLine +
         "2 | Joshua Durksen | PAR | Invicta Racing",
         text
      );
   }

   [Fact]
   public void ExtractRelevantImagesFromHtmlFindsDocumentStyleImage()
   {
      var html = """
         <html>
         <body>
         <img
            class="media-element file-fia-image-full content-details"
            src="/images/2026_f2_drivers_list.png"
            alt="">
         </body>
         </html>
         """;

      var images =
         WebPageContentFetchSupport.ExtractRelevantImagesFromHtml(
            html,
            new Uri("https://www.example.test/entry-list")
         );

      var image = Assert.Single(images);
      Assert.Equal(
         "https://www.example.test/images/2026_f2_drivers_list.png",
         image.Url
      );
   }






   [Theory]
   [InlineData("https://example.test/article")]
   [InlineData("http://8.8.8.8/article")]
   [InlineData("https://[2606:4700:4700::1111]/article")]
   public void UrlPolicyAllowsPublicWebUrls(string url)
   {
      var isValid = WebPageUrlPolicy.TryValidate(
         url,
         out var absoluteUrl,
         out var error
      );

      Assert.True(isValid, error);
      Assert.Equal(url, absoluteUrl.AbsoluteUri);
   }













   [Fact]
   public void ExtractEmbeddedStateKeepsRepeatedValuesWithTheirRecords()
   {
      const string html = """
         <html>
            <body>
               <script id="__NEXT_DATA__" type="application/json">
                  {
                     "players": [
                        {
                           "countryCode": "SWE",
                           "countryName": "Sweden",
                           "playerName": "Jesper Svensson"
                        },
                        {
                           "countryCode": "SWE",
                           "countryName": "Sweden",
                           "playerName": "Pontus Nyholm"
                        },
                        {
                           "countryCode": "NOR",
                           "countryName": "Norway",
                           "playerName": "Kristoffer Ventura"
                        }
                     ]
                  }
               </script>
            </body>
         </html>
         """;

      var text = WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(html);

      Assert.Contains(
         "countryCode: SWE | countryName: Sweden | " +
         "playerName: Jesper Svensson",
         text
      );
      Assert.Contains(
         "countryCode: SWE | countryName: Sweden | " +
         "playerName: Pontus Nyholm",
         text
      );
      Assert.Contains(
         "countryCode: NOR | countryName: Norway | " +
         "playerName: Kristoffer Ventura",
         text
      );
   }

   [Fact]
   public void ExtractEmbeddedStateSkipsPresentationConfiguration()
   {
      const string html = """
         <html>
            <body>
               <script type="application/json">
                  {
                     "excl_padd": "0 0 2px",
                     "f_vid_title_font_title":
                        "Video pop-up article title",
                     "articleTitle": "Useful article",
                     "categoryName": "Tennis"
                  }
               </script>
            </body>
         </html>
         """;

      var text = WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(html);

      Assert.DoesNotContain("excl_padd", text);
      Assert.DoesNotContain("Video pop-up article title", text);
      Assert.Contains(
         "articleTitle: Useful article | categoryName: Tennis",
         text
      );
   }

   [Fact]
   public void ExtractEmbeddedStateSkipsEncodedAndScriptConfiguration()
   {
      const string html = """
         <script type="application/json">
            {
               "articleTitle": "Useful article",
               "art_title": "eyJmb28iOiJiYXIifQ==",
               "imageExt": "jpg|jpeg|png",
               "valueName": "search_term_string",
               "scriptValue": "function replace(data-lazy-src)"
            }
         </script>
         """;

      var text = WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(html);

      Assert.Contains("Useful article", text);
      Assert.DoesNotContain("eyJmb28iOiJiYXIifQ==", text);
      Assert.DoesNotContain("jpg|jpeg|png", text);
      Assert.DoesNotContain("search_term_string", text);
      Assert.DoesNotContain("function replace", text);
   }






   [Fact]
   public void PublishedAtFallsBackToJsonLd()
   {
      const string html = """
         <script type="application/ld+json">
            {
               "@type": "NewsArticle",
               "datePublished": "2026-08-30T06:41:37+00:00"
            }
         </script>
         """;

      var publishedAt = WebPageContentFetchSupport.ExtractPublishedAt(html);

      Assert.Equal(
         DateTimeOffset.Parse("2026-08-30T06:41:37+00:00"),
         publishedAt
      );
   }




   [Theory]
   [InlineData("html")]
   [InlineData("curl")]
   public void BlockDetectionMatchesReferenceHashSignature(string sourceKind)
   {
      var source = ParseBlockSource(sourceKind);
      var blocked = WebPageBlockDetection.IsBlocked(
         "Error",
         "Reference #12345",
         source
      );

      Assert.True(blocked);
   }

   [Theory]
   [InlineData("html")]
   [InlineData("curl")]
   public void BlockDetectionAllowsReferenceGuideText(string sourceKind)
   {
      var source = ParseBlockSource(sourceKind);
      var blocked = WebPageBlockDetection.IsBlocked(
         "Reference Guide",
         "Please read the reference guide.",
         source
      );

      Assert.False(blocked);
   }

   [Fact]
   public void BrowserContextLeavesHeadersToPlaywright()
   {
      var options = WebPageBrowserPageFetcher.BuildContextOptions(
         "Mozilla/5.0 Chrome/143.0.0.0 Safari/537.36"
      );

      Assert.Null(options.ExtraHTTPHeaders);
      Assert.Equal(WebPageFetchDefaults.BrowserLocale, options.Locale);
      Assert.Equal(
         WebPageFetchDefaults.BrowserViewportWidth,
         options.ViewportSize!.Width
      );
   }

   [Fact]
   public void BrowserContextCanLeaveUserAgentToBrowserEngine()
   {
      var options = WebPageBrowserPageFetcher.BuildContextOptions();

      Assert.Null(options.UserAgent);
      Assert.Null(options.ExtraHTTPHeaders);
   }

   [Fact]
   public void BrowserBlockDetectionMatchesCloudflareVerificationPage()
   {
      var blocked = WebPageBlockDetection.IsBlocked(
         "Just a moment...",
         "Performing security verification. " +
         "This website uses a security service to protect against malicious " +
         "bots.",
         WebPageBlockSource.Browser
      );

      Assert.True(blocked);
   }

   [Fact]
   public void BrowserBlockDetectionAllowsOrdinarySecurityText()
   {
      var blocked = WebPageBlockDetection.IsBlocked(
         "Security guide",
         "This article explains browser security verification.",
         WebPageBlockSource.Browser
      );

      Assert.False(blocked);
   }

   [Fact]
   public void BrowserBlockDetectionMatchesAkamaiAccessDeniedPage()
   {
      var blocked = WebPageBlockDetection.IsBlocked(
         "Access Denied",
         "You don't have permission to access this page. " +
         "Reference #18.12345678.1234567890.abcdef01",
         WebPageBlockSource.Browser
      );

      Assert.True(blocked);
   }

   [Theory]
   [InlineData("html")]
   [InlineData("curl")]
   public void FallbackBlockDetectionMatchesCloudflareVerificationPage(
      string sourceKind
   )
   {
      var blocked = WebPageBlockDetection.IsBlocked(
         "Just a moment...",
         "Performing security verification.",
         ParseBlockSource(sourceKind)
      );

      Assert.True(blocked);
   }









   [Fact]
   public void NormalizeTextCollapsesAdjacentCountryNameDuplicates()
   {
      Assert.Equal(
         PrimaryCountry.CountryName,
         WebPageContentFetchSupport.NormalizeText(
            $"{PrimaryCountry.CountryName} {PrimaryCountry.CountryName}"
         )
      );
      Assert.Equal(
         PrimaryCountry.CountryName,
         WebPageContentFetchSupport.NormalizeText(
            $"{PrimaryCountry.CountryName} | {PrimaryCountry.CountryName}"
         )
      );
      Assert.Equal(
         "South Africa",
         WebPageContentFetchSupport.NormalizeText(
            "South Africa\nSouth Africa"
         )
      );
   }

   [Fact]
   public void NormalizeTextSanitizesPostgresUnsupportedUnicode()
   {
      var text = "Rally\0 Polen \uD800 😀";

      Assert.Equal(
         "Rally Polen � 😀",
         WebPageContentFetchSupport.NormalizeText(text)
      );
   }

   [Fact]
   public void NormalizeTextSeparatesGolfPlayerNameFromClub()
   {
      Assert.Equal(
         "Sweden LAGERGREN, Joakim | Black Mountain GC",
         WebPageContentFetchSupport.NormalizeText(
            "Sweden LAGERGREN, JoakimBlack Mountain GC"
         )
      );
      Assert.Equal(
         "Sweden TOWNSEND, Hugo | Stockholms GK",
         WebPageContentFetchSupport.NormalizeText(
            "Sweden TOWNSEND, HugoStockholms GK"
         )
      );
   }

   [Fact]
   public void NormalizeTextSeparatesNamesFromDuplicatedNextCellSuffix()
   {
      Assert.Equal(
         "Sweden NOREN, Alex | Troon | 60",
         WebPageContentFetchSupport.NormalizeText(
            "Sweden NOREN, AlexTroon | Troon | 60"
         )
      );
      Assert.Equal(
         "Sweden FORSSTRÖM, Simon | Gamebook | 11",
         WebPageContentFetchSupport.NormalizeText(
            "Sweden FORSSTRÖM, SimonGamebook | Gamebook | 11"
         )
      );
   }

   [Fact]
   public void NormalizeTextDropsStandaloneNoiseLines()
   {
      var text = """
         12
         fl
         Jurander Fanny
         18
         1
         90
         0
         0
         0
         0
         1
         BK Häcken
         """;

      Assert.Equal(
         "Jurander Fanny\nBK Häcken",
         WebPageContentFetchSupport.NormalizeText(text)
      );
   }

   [Fact]
   public void NormalizeTextCollapsesAdjacentNameFragmentsWithoutDuplication()
   {
      var text = """
         SWE
         Hanna
         Karlsson
         """;

      Assert.Equal(
         "SWE\nHanna Karlsson",
         WebPageContentFetchSupport.NormalizeText(text)
      );
   }

   [Fact]
   public void ExtractRelevantLinksFromHtmlPrefersMainBodyLink()
   {
      var entryListUrl =
         "https://registration.jstiming.com/events/" +
         "a0755ff4-4b6e-4d54-8566-caf947debd99/entries";
      var html = """
         <html>
            <body>
               <header>
                  <a href="/en">Home</a>
               </header>
               <main>
                  <h1>UEC BMX Championships</h1>
                  <p>
                     <a href="{0}">
                        Entry list
                     </a>
                  </p>
                  <p>
                     <a href="#details">Details</a>
                  </p>
               </main>
               <footer>
                  <a href="/privacy">Privacy</a>
               </footer>
            </body>
         </html>
         """;
      html = string.Format(html, entryListUrl);

      var links = WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
         html,
         new Uri(
            "https://www.uec.ch/en/event/274/" +
            "2026-uec-bmx-racing-european-championships"
         )
      );

      Assert.Single(links);
      Assert.Equal("Entry list", links[0].Label);
      Assert.Equal(
         "https://registration.jstiming.com/events/" +
         "a0755ff4-4b6e-4d54-8566-caf947debd99/entries",
         links[0].Url
      );
   }

   [Fact]
   public void ExtractRelevantLinksFromHtmlSkipsNoiseLinks()
   {
      var html = """
         <html>
            <body>
               <main>
                  <a href="/landslag/f07/f19-em/">
                     Resultat och spelschema EM
                  </a>
                  <a href="/go-to/?fplguid=123">
                     Saga Andersson
                     FC Rosengård Elitfotboll AB
                  </a>
                  <a href="/entries">
                     Entry list
                  </a>
               </main>
            </body>
         </html>
         """;

      var links = WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
         html,
         new Uri("https://www.svenskfotboll.se/nyheter/landslag/")
      );

      Assert.Single(links);
      Assert.Equal("Entry list", links[0].Label);
      Assert.Equal(
         "https://www.svenskfotboll.se/entries",
         links[0].Url
      );
   }

   [Fact]
   public void ExtractRelevantLinksFromHtmlAllowsCommonListTerms()
   {
      var html = """
         <html>
            <body>
               <main>
                  <a href="/roster">Roster</a>
                  <a href="/players">Players</a>
                  <a href="/competitors">Competitors</a>
                  <a href="/trupp">Trupp</a>
                  <a href="/squad">Squad</a>
               </main>
            </body>
         </html>
         """;

      var links = WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
         html,
         new Uri("https://www.example.test/event/")
      );

      Assert.Equal(5, links.Count);
      Assert.Contains(links, link => link.Label == "Roster");
      Assert.Contains(links, link => link.Label == "Players");
      Assert.Contains(links, link => link.Label == "Competitors");
      Assert.Contains(links, link => link.Label == "Trupp");
      Assert.Contains(links, link => link.Label == "Squad");
   }

   [Fact]
   public void ExtractRelevantLinksFromHtmlIncludesPdfLinks()
   {
      var html = """
         <html>
            <body>
               <main>
                  <h1>Final Start Lists</h1>
                  <table>
                     <tr>
                        <th>TIME</th>
                        <th>EVENT START LISTS PDF</th>
                     </tr>
                     <tr>
                        <td>16:30</td>
                        <td>
                           <a href="/files/men-pole-vault.pdf">
                              Pole Vault- men
                           </a>
                        </td>
                     </tr>
                  </table>
               </main>
            </body>
         </html>
         """;

      var links = WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
         html,
         new Uri("https://www.example.test/article/start-lists")
      );

      Assert.Single(links);
      Assert.Equal("Pole Vault- men", links[0].Label);
      Assert.Equal(
         "https://www.example.test/files/men-pole-vault.pdf",
         links[0].Url
      );
   }

   [Fact]
   public void ExtractRelevantLinksFromHtmlIncludesPdfLinksWithoutMainElement()
   {
      var href =
         "/userfiles/files/Continental%20Tour/" +
         "men-pole-vault-istvan-memorial.pdf";
      var html =
         "<html><body><div class=\"page-description\"><table>" +
         "<tr><th>TIME</th><th>EVENT START LISTS PDF</th></tr>" +
         "<tr><td>16:30</td><td>" +
         $"<a href=\"{href}\">Pole Vault- men</a>" +
         "</td></tr></table></div></body></html>";

      var links = WebPageContentFetchSupport.ExtractRelevantLinksFromHtml(
         html,
         new Uri(
            "https://www.watchathletics.com/article/13438/" +
            "final-start-lists"
         )
      );

      Assert.Single(links);
      Assert.Equal("Pole Vault- men", links[0].Label);
      Assert.Equal(
         "https://www.watchathletics.com/userfiles/files/" +
         "Continental%20Tour/men-pole-vault-istvan-memorial.pdf",
         links[0].Url
      );
   }

   [Fact]
   public void FormatPageContentTextOmitsNonPdfRelevantLinks()
   {
      var output = LlamaPageToolFormatter.FormatPageContentText(
         "Page URL",
         "https://example.test/article",
         "Title",
         "https://example.test/article",
         null,
         null,
         [],
         [
            new WebPageRelevantLink(
               "Entry list",
               "https://example.test/entries"
            )
         ],
         null,
         null,
         "Page body text."
      );

      Assert.DoesNotContain("Relevant links:", output);
      Assert.DoesNotContain("https://example.test/entries", output);
      Assert.Contains("Page text:", output);
   }

   [Fact]
   public void FormatPageContentTextPlacesPdfLinksBeforePageText()
   {
      var output = LlamaPageToolFormatter.FormatPageContentText(
         "Page URL",
         "https://example.test/article",
         "Title",
         "https://example.test/article",
         null,
         null,
         [],
         [
            new WebPageRelevantLink(
               "Pole Vault- men",
               "https://example.test/files/men-pole-vault.pdf"
            )
         ],
         null,
         null,
         "Page body text."
      );

      Assert.Contains("PDF links:", output);
      Assert.Contains(
         "- Pole Vault- men: https://example.test/files/men-pole-vault.pdf",
         output
      );
      Assert.True(
         output.IndexOf("PDF links:", StringComparison.Ordinal) <
         output.IndexOf("Page text:", StringComparison.Ordinal)
      );
   }

   [Fact]
   public async Task NormalizeFlagIconClassUsesCountryLabel()
   {
      var html = """
         <html>
            <body>
               <span class="flag-icon flag-icon-SE"></span>
            </body>
         </html>
         """;
      var normalizedText = await EvaluateNormalizationScriptAsync(html);

      Assert.Equal(PrimaryCountry.CountryName, normalizedText);
      Assert.DoesNotContain("icon", normalizedText, StringComparison.Ordinal);
   }

   [Fact]
   public async Task NormalizeFlagImageSourcePrefersCountryCode()
   {
      var html = """
         <html>
            <body>
               <img src="/images/flags/SE.png" alt="icon" />
            </body>
         </html>
         """;
      var normalizedText = await EvaluateNormalizationScriptAsync(html);

      Assert.Equal(PrimaryCountry.CountryName, normalizedText);
      Assert.DoesNotContain("icon", normalizedText, StringComparison.Ordinal);
   }

   [Fact]
   public async Task NormalizeTableRowsDeduplicatesFlagCountryCells()
   {
      var html = $$"""
         <html>
            <body>
               <table>
                  <tr>
                     <td class="table__cell--country">
                        <div>
                           <img
                              src="/Images/Flags/PRIMARY_18x18_1x.png"
                              alt="Flag for {{PrimaryCountry.ThreeLetterCode}}"
                              class="flag flag--outline" />
                        </div>
                     </td>
                     <td>{{PrimaryCountry.CountryName}}</td>
                     <td>LAGERGREN, JoakimBlack Mountain GC</td>
                     <td>Black Mountain GC</td>
                  </tr>
               </table>
            </body>
         </html>
         """;
      var normalizedText = await EvaluateNormalizationScriptAsync(html);

      Assert.Contains(
         $"{PrimaryCountry.CountryName} | LAGERGREN, Joakim | " +
         "Black Mountain GC",
         normalizedText,
         StringComparison.Ordinal
      );
      Assert.DoesNotContain(
         $"{PrimaryCountry.CountryName} | {PrimaryCountry.CountryName}",
         normalizedText,
         StringComparison.Ordinal
      );
   }

   [Fact]
   public async Task NormalizeTableRowsPreservesCompetitorBoundaries()
   {
      var html = CreateCompetitorTableHtml();
      var normalizedText = await EvaluateNormalizationScriptAsync(html);

      Assert.Contains(
         $"{PrimaryCountry.ThreeLetterCode} | FREDRICSON, Peder\n" +
         "ANDERSSON, Petronella\nBARYARD-JOHNSSON, Malin",
         normalizedText,
         StringComparison.Ordinal
      );
      Assert.DoesNotContain(
         "Peder ANDERSSON",
         normalizedText,
         StringComparison.Ordinal
      );
   }

   [Fact]
   public void HtmlTextPreservesNativeTableRowsWithoutFlatDuplicate()
   {
      var text = WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(CreateCompetitorTableHtml());

      Assert.Contains(
         $"{PrimaryCountry.ThreeLetterCode} | FREDRICSON, Peder\n" +
         "ANDERSSON, Petronella\nBARYARD-JOHNSSON, Malin",
         text,
         StringComparison.Ordinal
      );
      Assert.Equal(1, CountText(text, "FREDRICSON, Peder"));
      Assert.Equal(1, CountText(text, "ANDERSSON, Petronella"));
      Assert.Equal(1, CountText(text, "BARYARD-JOHNSSON, Malin"));
   }

   [Fact]
   public void ExtractHtmlTextKeepsFlagImageCountryLabel()
   {
      var html = $$"""
         <html>
            <body>
               <span>
                  <img
                     src="/Images/Flags/PRIMARY_18x18_1x.png"
                     width="18"
                     height="18"
                     alt="Flag for {{PrimaryCountry.ThreeLetterCode}}"
                     class="flag flag--outline"
                    srcset="/Images/Flags/PRIMARY_18x18_1x.png,
                       /Images/Flags/PRIMARY_18x18_2x.png 2x" />
                  Hanna Karlsson
               </span>
            </body>
         </html>
         """;

      var text = WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(html);

      Assert.Equal(
         $"{PrimaryCountry.CountryName}\nHanna Karlsson",
         text
      );
   }

   [Fact]
   public void ExtractHtmlTextKeepsProCyclingStatsFlagCountryLabel()
   {
      var html = $$"""
         <html>
            <body>
               <li>
                  <span class="bib">42</span>
                  <span class="flag
                     {{PrimaryCountry.TwoLetterCode.ToLowerInvariant()}}">
                  </span>
                  <a href="/rider/hanna-karlsson">KARLSSON Hanna</a>
               </li>
            </body>
         </html>
         """;

      var text = WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(html);

      Assert.Contains(PrimaryCountry.CountryName, text);
      Assert.Contains("KARLSSON Hanna", text);
   }

   [Fact]
   public void ExtractHtmlTextKeepsSvgFlagCountryLabel()
   {
      var html = $$"""
         <html>
            <body>
               <svg class="country-symbol">
                  <use href="#flag-{{PrimaryCountry.TwoLetterCode}}">
                  </use>
               </svg>
               Hanna Karlsson
            </body>
         </html>
         """;

      var text = WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(html);

      Assert.Contains(PrimaryCountry.CountryName, text);
      Assert.Contains("Hanna Karlsson", text);
   }

   [Fact]
   public async Task BrowserAndHtmlPathsShareSemanticNormalization()
   {
      var html = $$"""
         <html>
            <body>
               <nav>Navigation noise</nav>
               <table>
                  <tr>
                     <td>
                        <span class="flag
                           {{PrimaryCountry.TwoLetterCode
                              .ToLowerInvariant()}}">
                        </span>
                     </td>
                     <td>KARLSSON Hanna</td>
                  </tr>
               </table>
            </body>
         </html>
         """;
      var htmlText = WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(html);
      var browserText = await EvaluateNormalizationScriptAsync(html);

      Assert.Equal(htmlText, browserText);
      Assert.Contains(PrimaryCountry.CountryName, browserText);
      Assert.DoesNotContain("Navigation noise", browserText);
   }

   [Fact]
   public async Task BrowserAndHtmlPathsOmitSelectOptions()
   {
      var html = """
         <html>
            <body>
               <h1>Squad Sunderland AFC</h1>
               <label>Filter by season</label>
               <select name="season">
                  <option value="2026">26/27</option>
                  <option value="2002">02/03</option>
                  <option value="1961">60/61</option>
               </select>
               <p>Melker Ellborg</p>
            </body>
         </html>
         """;
      var htmlText = WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(html);
      var browserText = await EvaluateNormalizationScriptAsync(html);

      Assert.Equal(htmlText, browserText);
      Assert.Contains("Filter by season", browserText);
      Assert.Contains("Melker Ellborg", browserText);
      Assert.DoesNotContain("26/27", browserText);
      Assert.DoesNotContain("02/03", browserText);
      Assert.DoesNotContain("60/61", browserText);
   }

   [Fact]
   public async Task NormalizeWikipediaFlagImageUsesAltText()
   {
      var html =
         "<html><body><table><tbody><tr><td>" +
         "<span class=\"flagicon\">" +
         "<span class=\"mw-image-border\" typeof=\"mw:File\">" +
         "<a href=\"/wiki/Argentina\" title=\"Argentina\">" +
         "<img alt=\"Argentina\" " +
         "src=\"//upload.wikimedia.org/wikipedia/commons/thumb/1/1a/" +
         "Flag_of_Argentina.svg/40px-Flag_of_Argentina.svg.png\" />" +
         "</a></span></span> Luciano Martinez" +
         "</td></tr></tbody></table></body></html>";
      var normalizedText = await EvaluateNormalizationScriptAsync(html);

      Assert.Contains(
         "Argentina",
         normalizedText,
         StringComparison.OrdinalIgnoreCase
      );
      Assert.Contains(
         "Luciano Martinez",
         normalizedText,
         StringComparison.OrdinalIgnoreCase
      );
      Assert.DoesNotContain(" of ", normalizedText, StringComparison.Ordinal);
   }

   [Fact]
   public async Task NormalizeWikipediaFlagImageSourceSkipsOfNoise()
   {
      var html =
         "<html><body><img " +
         "src=\"//upload.wikimedia.org/wikipedia/commons/thumb/1/1a/" +
         "Flag_of_Argentina.svg/40px-Flag_of_Argentina.svg.png\" />" +
         "</body></html>";
      var normalizedText = await EvaluateNormalizationScriptAsync(html);

      Assert.Equal("Argentina", normalizedText);
   }

   [Fact]
   public void BuildBrowserUserAgentUsesBrowserMajorVersion()
   {
      var userAgent = WebPageContentFetchSupport.BuildBrowserUserAgent(
         "HeadlessChrome/143.0.7499.0"
      );

      Assert.StartsWith("Mozilla/5.0", userAgent);
      Assert.Contains("Chrome/143.0.0.0", userAgent);
      Assert.EndsWith("Safari/537.36", userAgent);
   }

   [Fact]
   public void ApplyResponseCutoffAppendsMarkerToTruncatedText()
   {
      var text = new string(
         'x',
         WebPageFetchDefaults.MaxResponseCharacters + 1
      );

      var result = WebPageContentFetchSupport.ApplyResponseCutoff(text);

      Assert.EndsWith(WebPageFetchDefaults.CutoffMarker, result);
      Assert.Equal(
         WebPageFetchDefaults.MaxResponseCharacters,
         result.Length
      );
   }

   [Fact]
   public void ApplyResponseCutoffLeavesShortTextUntouched()
   {
      var text = "Short text.";

      var result = WebPageContentFetchSupport.ApplyResponseCutoff(text);

      Assert.Equal(text, result);
   }

   [Fact]
   public void GetCountryDisplayNameUsesNetRegionInfo()
   {
      Assert.Equal(
         PrimaryCountry.CountryName,
        WebPageContentFetchSupport.GetCountryDisplayName(
            PrimaryCountry.TwoLetterCode
         )
      );
      Assert.Equal(
         PrimaryCountry.CountryName,
         WebPageContentFetchSupport.GetCountryDisplayName(
            PrimaryCountry.ThreeLetterCode
         )
      );
      Assert.Equal(
         "Norway",
         WebPageContentFetchSupport.GetCountryDisplayName("NO")
      );
      Assert.Equal(
         "Spain",
         WebPageContentFetchSupport.GetCountryDisplayName("ES")
      );
      Assert.Equal(
         "Belgium",
         WebPageContentFetchSupport.GetCountryDisplayName("BEL")
      );
      Assert.Null(WebPageContentFetchSupport.GetCountryDisplayName("??"));
   }

   private static string CreateCompetitorTableHtml()
   {
      return $$"""
         <html>
            <body>
               <table>
                  <tr>
                     <td>{{PrimaryCountry.ThreeLetterCode}}</td>
                     <td>FREDRICSON, Peder</td>
                  </tr>
                  <tr>
                     <td></td>
                     <td>ANDERSSON, Petronella</td>
                  </tr>
                  <tr>
                     <td></td>
                     <td>BARYARD-JOHNSSON, Malin</td>
                  </tr>
               </table>
            </body>
         </html>
         """;
   }

   private static int CountText(string text, string value)
   {
      var count = 0;
      var startIndex = 0;

      while((startIndex = text.IndexOf(
         value,
         startIndex,
         StringComparison.Ordinal
      )) >= 0)
      {
         count++;
         startIndex += value.Length;
      }

      return count;
   }

   private static WebPageBlockSource ParseBlockSource(string sourceKind)
   {
      return string.Equals(
         sourceKind,
         "curl",
         StringComparison.OrdinalIgnoreCase
      )
         ? WebPageBlockSource.CurlFallback
         : WebPageBlockSource.HtmlFallback;
   }

   private static async Task<string> EvaluateNormalizationScriptAsync(
      string html
   )
   {
      using var playwright = await Playwright.CreateAsync();
      await using var browser = await playwright.Chromium.LaunchAsync(
         new BrowserTypeLaunchOptions
         {
            Headless = true
         }
      );
      await using var context = await browser.NewContextAsync();
      await using var page = await context.NewPageAsync();

      await page.SetContentAsync(html);
      await page.EvaluateAsync(WebPageNormalizationScript.Build());
      var bodyHtml = await page.Locator("body").EvaluateAsync<string>(
         "element => element.innerHTML"
      );

      return WebPageContentFetchSupport
         .ExtractHtmlTextWithEmbeddedState(bodyHtml);
   }


}
