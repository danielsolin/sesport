using System.Net;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using SESport.AI.Providers;
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

      Assert.Equal(3, browserCalls);
      Assert.NotNull(page);
      Assert.Equal(WebPageFetchErrorKind.Timeout, page!.FetchErrorKind);
      Assert.Equal(
         "HTML fallback produced no text.",
         page!.FetchErrorMessage
      );
   }

   [Fact]
   public async Task FetchRetriesWhenInitialRequestTimesOut()
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

      Assert.Equal(2, handler.RequestCount);
      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("Retry Title", page!.Title);
   }

   [Fact]
   public async Task FetchRetriesWhenBrowserTimesOut()
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

      Assert.Equal(2, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("Retry Title", page!.Title);
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
         "South Africa",
         WebPageContentFetchSupport.NormalizeText(
            "South Africa\nSouth Africa"
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
      Assert.Equal("Sweden", WebPageContentClient.GetCountryDisplayName("SE"));
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
