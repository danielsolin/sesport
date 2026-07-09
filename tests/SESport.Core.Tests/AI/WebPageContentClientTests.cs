using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using SESport.AI.Llama;
using SESport.AI.WebPages;
using System.Net;
using System.Text.Json;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace SESport.Core.Tests.AI;

public class WebPageContentClientTests
{
   private static readonly Func<Task<string>> BrowserUserAgentProvider =
      () => Task.FromResult(
         "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
         "(KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36"
      );

   [Fact]
   public async Task FetchReturnsBrowserContent()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler()),
         (_, _) =>
         {
            browserCalls++;

            return Task.FromResult<WebPageContent?>(
               new WebPageContent(
                  "Browser Title",
                  "https://example.test/article",
                  DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
                  [],
                  "Browser body text.",
                  true,
                  "Browser body text."
               )
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/article",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("Browser Title", page!.Title);
      Assert.Equal("Browser body text.", page.MainText);
   }

   [Fact]
   public async Task FetchReturnsNullForInvalidUrl()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(),
         (_, _) =>
         {
            browserCalls++;
            return Task.FromResult<WebPageContent?>(null);
         }
      );

      var page = await client.FetchAsync(
         "not a url",
         CancellationToken.None
      );

      Assert.Equal(0, browserCalls);
      Assert.Null(page);
   }

   [Fact]
   public async Task FetchReturnsNullWhenBrowserReturnsNull()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler()),
         (_, _) =>
         {
            browserCalls++;
            return Task.FromResult<WebPageContent?>(null);
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/empty",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.Null(page);
   }

   [Fact]
   public async Task FetchReturnsErrorContentWhenBrowserTimesOut()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler()),
         (_, _) =>
         {
            browserCalls++;
            throw new TimeoutException("Timeout 30000ms exceeded.");
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/slow",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal(WebPageFetchErrorKind.Timeout, page!.FetchErrorKind);
      Assert.Equal(
         "HTML fallback produced no text.",
         page!.FetchErrorMessage
      );
   }

   [Fact]
   public async Task FetchReturnsErrorContentWhenPrimaryRequestFails()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(new ThrowingHandler(
            new HttpRequestException(
               "The SSL connection could not be established."
            )
         )),
         (_, _) =>
         {
            browserCalls++;
            throw new PlaywrightException("Browser blocked");
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/ssl",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal(
         WebPageFetchErrorKind.BrowserBlocked,
         page!.FetchErrorKind
      );
      Assert.Equal(
         "Could not retrieve page content from " +
         "https://example.test/ssl.",
         page.FetchErrorMessage
      );
   }

   [Fact]
   public async Task FetchFallsBackWhenInitialRequestTimesOut()
   {
      var browserCalls = 0;
      var handler = new FlakyTimeoutHandler();
      var client = CreateClient(
         new HttpClient(handler)
         {
            Timeout = TimeSpan.FromMilliseconds(50)
         },
         (_, _) =>
         {
            browserCalls++;
            return Task.FromResult<WebPageContent?>(
               new WebPageContent(
                  "Retry Title",
                  "https://example.test/retry",
                  null,
                  [],
                  "Retry body text.",
                  true,
                  "Retry body text."
               )
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/retry",
         CancellationToken.None
      );

      Assert.Equal(1, handler.RequestCount);
      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("Retry Title", page!.Title);
   }

   [Fact]
   public async Task FetchFallsBackWhenBrowserTimesOut()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(string.Empty)),
         (_, _) =>
         {
            browserCalls++;

            if(browserCalls == 1)
            {
               throw new TimeoutException("Timeout 30000ms exceeded.");
            }

            return Task.FromResult<WebPageContent?>(
               new WebPageContent(
                  "Retry Title",
                  "https://example.test/browser-timeout",
                  null,
                  [],
                  "Retry body text.",
                  true,
                  "Retry body text."
               )
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/browser-timeout",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal(WebPageFetchErrorKind.Timeout, page!.FetchErrorKind);
      Assert.Equal("HTML fallback had no body.", page!.FetchErrorMessage);
   }

   [Fact]
   public async Task FetchUsesCurlFallbackWhenHtmlFallbackIsEmpty()
   {
      var browserCalls = 0;
      var curlCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(string.Empty)),
         (_, _) =>
         {
            browserCalls++;
            throw new PlaywrightException("Browser blocked");
         },
         (_, _) =>
         {
            curlCalls++;
            return Task.FromResult<WebPageContent?>(
               new WebPageContent(
                  "Curl Title",
                  "https://example.test/curl",
                  null,
                  [],
                  "Curl body text.",
                  true,
                  "Curl body text."
               )
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/curl",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.Equal(1, curlCalls);
      Assert.NotNull(page);
      Assert.Equal("Curl Title", page!.Title);
      Assert.Equal("Curl body text.", page.MainText);
   }

   [Fact]
   public async Task FetchFallsBackToHtmlWhenBrowserFails()
   {
      var browserCalls = 0;
      var handler = new HtmlRecordingHandler(
         """
         <html>
            <head>
               <title>Fallback Title</title>
            </head>
            <body>
               <main>
                  <h1>Fallback heading</h1>
                  <p>Fallback body text.</p>
               </main>
            </body>
         </html>
         """
      );
      var client = CreateClient(
         new HttpClient(handler),
         (_, _) =>
         {
            browserCalls++;
            throw new PlaywrightException("Browser blocked");
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/fallback",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("Fallback Title", page!.Title);
      Assert.Contains("Fallback heading", page.MainText);
      Assert.Contains("Fallback body text.", page.MainText);
   }

   [Fact]
   public async Task FetchExtractsEmbeddedStateFromHtmlFallback()
   {
      var browserCalls = 0;
      var handler = new HtmlRecordingHandler(
         """
         <html>
            <head>
               <title>Embedded State Title</title>
            </head>
            <body>
               <div>Visible body text.</div>
               <script type="application/json" id="embedded-state">
                  {
                     "event": {
                        "title": "Embedded Event"
                     },
                     "participants": [
                        { "name": "Oliver Solberg" },
                        { "name": "Mille Johansson" },
                        { "name": "https://example.test/ignore" }
                     ]
                  }
               </script>
            </body>
         </html>
         """
      );
      var client = CreateClient(
         new HttpClient(handler),
         (_, _) =>
         {
            browserCalls++;
            throw new PlaywrightException("Browser blocked");
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/embedded-state",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("Embedded State Title", page!.Title);
      Assert.Contains("Oliver Solberg", page.MainText);
      Assert.Contains("Mille Johansson", page.MainText);
      Assert.Contains("Visible body text.", page.MainText);
   }

   [Fact]
   public async Task FetchReturnsErrorContentWhenFallbackHasNoBody()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(string.Empty)),
         (_, _) =>
         {
            browserCalls++;
            throw new PlaywrightException("Browser blocked");
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/fallback-error",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal(
         WebPageFetchErrorKind.BrowserBlocked,
         page!.FetchErrorKind
      );
      Assert.Equal(
         "HTML fallback had no body.",
         page!.FetchErrorMessage
      );
      Assert.False(page.HasBodyText);
   }

   [Fact]
   public async Task HtmlFetcherMarksReturnedContent()
   {
      var response = new HttpResponseMessage(HttpStatusCode.OK)
      {
         Content = new StringContent(
            """
            <html>
               <head>
                  <title>HTML Title</title>
               </head>
               <body>
                  <main>
                     <p>HTML body text.</p>
                  </main>
               </body>
            </html>
            """
         )
      };

      var page = await WebPageHtmlPageFetcher.FetchAsync(
         NullLogger.Instance,
         (_, _) => Task.FromResult<WebPageContent?>(null),
         response,
         new Uri("https://example.test/html"),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal("html", page!.Fetcher);
   }

   [Fact]
   public async Task HtmlFetcherAllowsReferenceText()
   {
      var response = new HttpResponseMessage(HttpStatusCode.OK)
      {
         Content = new StringContent(
            """
            <html>
               <head>
                  <title>Reference Guide</title>
               </head>
               <body>
                  <main>
                     <p>Please read the reference guide.</p>
                  </main>
               </body>
            </html>
            """
         )
      };

      var page = await WebPageHtmlPageFetcher.FetchAsync(
         NullLogger.Instance,
         (_, _) => Task.FromResult<WebPageContent?>(null),
         response,
         new Uri("https://example.test/reference"),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal("Reference Guide", page!.Title);
      Assert.Contains("reference guide", page.MainText);
      Assert.Equal("html", page.Fetcher);
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
   public async Task FetchExtractsTextFromPdfResponses()
   {
      var browserCalls = 0;
      var handler = new PdfRecordingHandler(CreatePdfBytes());
      var client = CreateClient(
         new HttpClient(handler),
         (_, _) =>
         {
            browserCalls++;
            return Task.FromResult<WebPageContent?>(null);
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/entry-list.pdf",
         CancellationToken.None
      );

      Assert.Equal(0, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("Sample PDF Title", page!.Title);
      Assert.Contains("Hello from the PDF body.", page.MainText);
   }

   [Fact]
   public async Task PdfFetcherMarksReturnedContent()
   {
      var response = new HttpResponseMessage(HttpStatusCode.OK)
      {
         Content = new ByteArrayContent(CreatePdfBytes())
      };

      var page = await WebPagePdfPageFetcher.FetchAsync(
         response,
         new Uri("https://example.test/sample.pdf"),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal("pdf", page!.Fetcher);
   }

   [Fact]
   public async Task FetchPreservesPdfLineBreaks()
   {
      var handler = new PdfRecordingHandler(CreateMultiLinePdfBytes());
      var client = CreateClient(
         new HttpClient(handler),
         (_, _) =>
         {
            return Task.FromResult<WebPageContent?>(null);
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/multiline.pdf",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains("First PDF line.", page!.MainText);
      Assert.Contains("Second PDF line.", page.MainText);
      Assert.Contains(Environment.NewLine, page.MainText);
   }

   [Fact]
   public async Task FetchPassesHtmlUrlToBrowserFetcher()
   {
      Uri? seenUrl = null;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler()),
         (url, _) =>
         {
            seenUrl = url;
            return Task.FromResult<WebPageContent?>(
               new WebPageContent(
                  "HTML Title",
                  url.ToString(),
                  null,
                  [],
                  "HTML body text.",
                  true,
                  "HTML body text."
               )
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/article",
         CancellationToken.None
      );

      Assert.NotNull(seenUrl);
      Assert.Equal("https://example.test/article", seenUrl!.ToString());
      Assert.NotNull(page);
      Assert.Equal("HTML Title", page!.Title);
      Assert.Contains("HTML body text.", page.MainText);
   }

   [Fact]
   public async Task FetchSendsBrowserLikeHeaders()
   {
      HttpRequestMessage? seenRequest = null;
      var client = CreateClient(
         new HttpClient(new RecordingHandler(request =>
         {
            seenRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
               Content = new StringContent("<html></html>")
            };
         })),
         (_, _) =>
         {
            return Task.FromResult<WebPageContent?>(null);
         }
      );

      await client.FetchAsync(
         "https://example.test/article",
         CancellationToken.None
      );

      Assert.NotNull(seenRequest);
      Assert.NotNull(seenRequest!.Headers.Accept);
      Assert.Contains(
         seenRequest.Headers.Accept,
         value => string.Equals(
            value.MediaType,
            "text/html",
            StringComparison.OrdinalIgnoreCase
         )
      );
      Assert.True(
         seenRequest.Headers.TryGetValues("Accept-Language", out var languages)
      );
      Assert.Contains("en-US", languages);
      Assert.Contains("en; q=0.9", languages);
      Assert.True(
         seenRequest.Headers.TryGetValues(
            "Upgrade-Insecure-Requests",
            out var upgrades
         )
      );
      Assert.Contains("1", upgrades);
      Assert.Contains(
         "Chrome/143.0.0.0",
         seenRequest.Headers.UserAgent.ToString()
      );
   }

   [Fact]
   public async Task FetchNormalizesFlagImageFromLivePlayerList()
   {
      if(!ShouldRunLiveWebPageTests())
      {
         return;
      }

      using var httpClient = new HttpClient
      {
         Timeout = TimeSpan.FromSeconds(90)
      };
      var client = CreateClient(httpClient);

      var page = await client.FetchAsync(
         "https://www.anwagolf.com/en_US/players/player_list.html",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains(
         "Sweden",
         page!.MainText,
         StringComparison.OrdinalIgnoreCase
      );
      Assert.Contains("SWE", page.MainText);
      Assert.DoesNotContain(
         "SWE_sm",
         page.MainText,
         StringComparison.OrdinalIgnoreCase
      );
   }

   [Fact]
   public void NormalizeTextCollapsesAdjacentCountryNameDuplicates()
   {
      Assert.Equal(
         "Sweden",
         WebPageContentFetchSupport.NormalizeText("Sweden Sweden")
      );
      Assert.Equal(
         "Sweden",
         WebPageContentFetchSupport.NormalizeText("Sweden | Sweden")
      );
      Assert.Equal(
         "South Africa",
         WebPageContentFetchSupport.NormalizeText(
            "South Africa\nSouth Africa"
         )
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
   public void FormatPageContentTextPlacesRelevantLinksBeforePageText()
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

      Assert.Contains("Relevant links:", output);
      Assert.Contains("- Entry list: https://example.test/entries", output);
      Assert.True(
         output.IndexOf("Relevant links:", StringComparison.Ordinal) <
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
      var userAgent = WebPageContentClient.BuildBrowserUserAgent(
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
         WebPageContentClient.MaxResponseCharacters + 1
      );

      var result = WebPageContentClient.ApplyResponseCutoff(text);

      Assert.EndsWith("[CUTOFF]", result);
      Assert.Equal(
         WebPageContentClient.MaxResponseCharacters,
         result.Length
      );
   }

   [Fact]
   public void ApplyResponseCutoffLeavesShortTextUntouched()
   {
      var text = "Short text.";

      var result = WebPageContentClient.ApplyResponseCutoff(text);

      Assert.Equal(text, result);
   }

   [Fact]
   public void GetCountryDisplayNameUsesNetRegionInfo()
   {
      Assert.Equal(
         PrimaryCountry.CountryName,
         WebPageContentClient.GetCountryDisplayName(
            PrimaryCountry.TwoLetterCode
         )
      );
      Assert.Equal(
         PrimaryCountry.CountryName,
         WebPageContentClient.GetCountryDisplayName(
            PrimaryCountry.ThreeLetterCode
         )
      );
      Assert.Equal("Norway", WebPageContentClient.GetCountryDisplayName("NO"));
      Assert.Equal("Spain", WebPageContentClient.GetCountryDisplayName("ES"));
      Assert.Equal(
         "Belgium",
         WebPageContentClient.GetCountryDisplayName("BEL")
      );
      Assert.Null(WebPageContentClient.GetCountryDisplayName("??"));
   }

   private static byte[] CreatePdfBytes()
   {
      var builder = new PdfDocumentBuilder();
      var page = builder.AddPage(PageSize.A4);
      var font = builder.AddStandard14Font(Standard14Font.Helvetica);
      builder.DocumentInformation.Title = "Sample PDF Title";
      page.AddText("Hello from the PDF body.", 12, new PdfPoint(72, 720), font);
      return builder.Build();
   }

   private static byte[] CreateMultiLinePdfBytes()
   {
      var builder = new PdfDocumentBuilder();
      var page = builder.AddPage(PageSize.A4);
      var font = builder.AddStandard14Font(Standard14Font.Helvetica);
      builder.DocumentInformation.Title = "Multi Line PDF";
      page.AddText("First PDF line.", 12, new PdfPoint(72, 720), font);
      page.AddText("Second PDF line.", 12, new PdfPoint(72, 700), font);
      return builder.Build();
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
      await page.EvaluateAsync(
         WebPageNormalizationScript.Build(),
         JsonSerializer.Serialize(
            WebPageContentFetchSupport.CountryNamesByCode
         )
      );

      return await page.Locator("body").InnerTextAsync();
   }

   private sealed class PdfRecordingHandler : HttpMessageHandler
   {
      private readonly byte[] content;

      public PdfRecordingHandler(byte[] content)
      {
         this.content = content;
      }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         var response = new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new ByteArrayContent(content)
         };
         response.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
               "application/pdf"
            );

         return Task.FromResult(response);
      }
   }

   private sealed class HtmlRecordingHandler : HttpMessageHandler
   {
      private readonly string content;

      public HtmlRecordingHandler(
         string content = "<html></html>"
      )
      {
         this.content = content;
      }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         var response = new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new StringContent(content)
         };
         response.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
               "text/html"
            );

         return Task.FromResult(response);
      }
   }

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly Func<HttpRequestMessage, HttpResponseMessage> respond;

      public RecordingHandler(
         Func<HttpRequestMessage, HttpResponseMessage> respond
      )
      {
         this.respond = respond;
      }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult(respond(request));
      }
   }

   private sealed class ThrowingHandler : HttpMessageHandler
   {
      private readonly Exception exception;

      public ThrowingHandler(Exception exception)
      {
         this.exception = exception;
      }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         _ = request;
         _ = cancellationToken;
         return Task.FromException<HttpResponseMessage>(exception);
      }
   }

   private sealed class FlakyTimeoutHandler : HttpMessageHandler
   {
      public int RequestCount { get; private set; }

      protected override async Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestCount++;

         if(RequestCount == 1)
         {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
         }

         return new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new StringContent("<html></html>")
         };
      }
   }

   private static WebPageContentClient CreateClient(
      HttpClient httpClient,
      Func<Uri, CancellationToken, Task<WebPageContent?>>? browserFetcher =
         null,
      Func<Uri, CancellationToken, Task<WebPageContent?>>? curlFetcher =
         null
   )
   {
      return new WebPageContentClient(
         httpClient,
         browserFetcher,
         null,
         BrowserUserAgentProvider,
         curlFetcher ?? ((_, _) => Task.FromResult<WebPageContent?>(null))
      );
   }

   private static bool ShouldRunLiveWebPageTests()
   {
      return string.Equals(
         Environment.GetEnvironmentVariable(
            "SESPORT_RUN_LIVE_WEBPAGE_TESTS"
         ),
         "1",
         StringComparison.OrdinalIgnoreCase
      );
   }
}
