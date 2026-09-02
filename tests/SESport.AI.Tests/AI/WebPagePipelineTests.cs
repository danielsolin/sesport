using System.Net;
using System.Net.Sockets;
using System.Text;

using Microsoft.Extensions.Logging.Abstractions;

using SESport.AI.WebPages;

using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace SESport.Core.Tests.AI;

/// <summary>
/// Tests for the unified fetch pipeline: direct HTTP, browser and curl
/// transports return structured evidence that
/// <see cref="WebPageFetchOrchestrator"/> turns into one decision tree.
/// </summary>
public class WebPagePipelineTests
{
   private const string TestBrowserUserAgent =
      "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
      "(KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36";

   [Fact]
   public async Task FetchReturnsNullForInvalidUrl()
   {
      var client = CreateClient(new HttpClient());

      var page = await client.FetchAsync(
         "not a url",
         CancellationToken.None
      );

      Assert.Null(page);
   }

   [Theory]
   [InlineData("http://localhost/admin")]
   [InlineData("http://service.localhost/admin")]
   [InlineData("http://10.0.0.1/admin")]
   [InlineData("http://169.254.169.254/metadata")]
   [InlineData("http://192.168.1.10/admin")]
   [InlineData("http://[::1]/admin")]
   [InlineData("http://[fe80::1]/admin")]
   [InlineData("file:///etc/passwd")]
   public async Task FetchRejectsNonPublicUrls(string url)
   {
      var client = CreateClient(new HttpClient());

      var page = await client.FetchAsync(url, CancellationToken.None);

      Assert.Null(page);
   }

   [Fact]
   public async Task FetchSucceedsWithRichHtmlWithoutBrowser()
   {
      var browserCalls = 0;
      var curlCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(RichHtmlDocument())),
         (url, strategies, token) =>
         {
            browserCalls++;
            return Task.FromResult(NoRenderOutcome());
         },
         (url, maxTime, token) =>
         {
            curlCalls++;
            return Task.FromResult(WebPageHttpResponse.Failure(
               url,
               "curl not available in test"
            ));
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/rich",
         CancellationToken.None
      );

      Assert.Equal(0, browserCalls);
      Assert.Equal(0, curlCalls);
      Assert.NotNull(page);
      Assert.Equal("http", page!.Fetcher);
      Assert.True(page.HasBodyText);
      Assert.Null(page.FetchErrorMessage);
   }

   [Fact]
   public async Task FetchUsesBrowserWhenHtmlIsShort()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(ShortHtmlDocument())),
         (url, strategies, token) =>
         {
            browserCalls++;
            return Task.FromResult(
               BrowserRender("Report", RichBodyHtml())
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/short",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("playwright", page!.Fetcher);
      Assert.Equal("chromium-bundled", page.BrowserStrategy);
      Assert.True(page.HasBodyText);
      Assert.Null(page.FetchErrorMessage);
   }

   [Fact]
   public async Task FetchReturnsPartialContentWhenAllStagesAreWeak()
   {
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(ShortHtmlDocument())),
         BrowserNoRenderFunc,
         CurlFailure
      );

      var page = await client.FetchAsync(
         "https://example.test/weak",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal("partial", page!.Fetcher);
      Assert.True(page.HasBodyText);
      Assert.Contains("Short body text.", page.MainText);
      Assert.Null(page.FetchErrorMessage);
      Assert.NotNull(page.RenderWarning);
      Assert.Contains("incomplete", page.RenderWarning,
         StringComparison.OrdinalIgnoreCase
      );
   }

   [Fact]
   public async Task FetchTriesBrowserForForbiddenResponse()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Forbidden)
         )),
         (url, strategies, token) =>
         {
            browserCalls++;
            return Task.FromResult(
               BrowserRender("Report", RichBodyHtml())
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/forbidden",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("playwright", page!.Fetcher);
      Assert.True(page.HasBodyText);
   }

   [Fact]
   public async Task FetchTriesCurlForForbiddenResponse()
   {
      var curlCalls = 0;
      var client = CreateClient(
         new HttpClient(new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Forbidden)
         )),
         BrowserNoRenderFunc,
         (url, maxTime, token) =>
         {
            curlCalls++;
            return Task.FromResult(CurlHtmlResponse(
               url,
               200,
               RichBodyHtml()
            ));
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/forbidden-curl",
         CancellationToken.None
      );

      Assert.Equal(1, curlCalls);
      Assert.NotNull(page);
      Assert.Equal("curl", page!.Fetcher);
      Assert.True(page.HasBodyText);
   }

   [Fact]
   public async Task FetchTriesBrowserWhenDirectHttpFails()
   {
      var browserCalls = 0;
      var client = CreateClient(
         new HttpClient(new ThrowingHandler(
            new HttpRequestException(
               "The SSL connection could not be established."
            )
         )),
         (url, strategies, token) =>
         {
            browserCalls++;
            return Task.FromResult(
               BrowserRender("Report", RichBodyHtml())
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/ssl",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("playwright", page!.Fetcher);
      Assert.True(page.HasBodyText);
   }

   [Fact]
   public async Task FetchReturnsFailureWithLedgerWhenAllStagesFail()
   {
      var client = CreateClient(
         new HttpClient(new ThrowingHandler(
            new HttpRequestException("Connection refused.")
         )),
         BrowserNoRenderFunc,
         CurlFailure
      );

      var page = await client.FetchAsync(
         "https://example.test/down",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal(
         WebPageFetchErrorKind.HttpError,
         page!.FetchErrorKind
      );
      Assert.False(page.HasBodyText);
      Assert.Contains("[http]", page.FetchErrorMessage);
      Assert.Contains("[browser:chromium-bundled]",
         page.FetchErrorMessage
      );
      Assert.Contains("[curl]", page.FetchErrorMessage);
   }

   [Fact]
   public async Task FetchRetriesTransientHttpErrors()
   {
      var attempts = 0;
      var client = CreateClient(
         new HttpClient(new RecordingHandler(_ =>
         {
            attempts++;
            return attempts < 2
               ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
               : new HttpResponseMessage(HttpStatusCode.OK)
               {
                  Content = new StringContent(RichHtmlDocument())
               };
         }))
      );

      var page = await client.FetchAsync(
         "https://example.test/flaky",
         CancellationToken.None
      );

      Assert.Equal(2, attempts);
      Assert.NotNull(page);
      Assert.Equal("http", page!.Fetcher);
      Assert.True(page.HasBodyText);
   }

   [Fact]
   public async Task FetchPropagatesCallerCancellation()
   {
      var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var client = CreateClient(
         new HttpClient(new GateHandler(gate.Task))
      );

      using var cts = new CancellationTokenSource();
      var fetchTask = client.FetchAsync(
         "https://example.test/slow",
         cts.Token
      );

      await Task.Delay(100);
      cts.Cancel();

      await Assert.ThrowsAnyAsync<OperationCanceledException>(
         () => fetchTask
      );
   }

   [Fact]
   public async Task FetchUsesBrowserWhenHtmlHasPlaceholders()
   {
      var browserCalls = 0;
      var shellHtml =
         """
         <html>
            <head><title>Shell</title></head>
            <body><main>
               <h1>Shell heading</h1>
               <p>TBD TBD TBD</p>
            </main></body>
         </html>
         """;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(shellHtml)),
         (url, strategies, token) =>
         {
            browserCalls++;
            return Task.FromResult(
               BrowserRender("Report", RichBodyHtml())
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/shell",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("playwright", page!.Fetcher);
   }

   [Fact]
   public async Task FetchRetainsNotFoundWhenCurlConfirmationFails()
   {
      var curlCalls = 0;
      var html =
         """
         <html>
            <head><title>Official site</title></head>
            <body><main>
               <h1>Let!</h1>
               <p>This page does not exist.</p>
            </main></body>
         </html>
         """;
      var client = CreateClient(
         new HttpClient(new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
               Content = new StringContent(html)
            }
         )),
         BrowserNoRenderFunc,
         (url, maxTime, token) =>
         {
            curlCalls++;
            return Task.FromResult(WebPageHttpResponse.Failure(
               url,
               "curl not available in test"
            ));
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/gone",
         CancellationToken.None
      );

      // Direct 404 evidence is retained when independent confirmation fails.
      Assert.Equal(1, curlCalls);
      Assert.NotNull(page);
      Assert.Equal(
         WebPageFetchErrorKind.HttpError,
         page!.FetchErrorKind
      );
      Assert.False(page.HasBodyText);
      Assert.Contains("not found", page.FetchErrorMessage,
         StringComparison.OrdinalIgnoreCase
      );
   }

   [Fact]
   public async Task FetchConfirmsNotFoundWhenCurlAlso404()
   {
      var client = CreateClient(
         new HttpClient(new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
         )),
         BrowserNoRenderFunc,
         (url, maxTime, token) =>
         {
            return Task.FromResult(CurlHtmlResponse(
               url,
               404,
               "<html><body></body></html>"
            ));
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/missing",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal(
         WebPageFetchErrorKind.HttpError,
         page!.FetchErrorKind
      );
      Assert.False(page.HasBodyText);
      Assert.Contains("not found", page.FetchErrorMessage,
         StringComparison.OrdinalIgnoreCase
      );
   }

   [Fact]
   public async Task FetchRecoversWhenCurlSucceedsAfterDirect404()
   {
      var client = CreateClient(
         new HttpClient(new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
         )),
         BrowserNoRenderFunc,
         (url, maxTime, token) =>
         {
            return Task.FromResult(CurlHtmlResponse(
               url,
               200,
               RichBodyHtml()
            ));
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/redirected-away",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal("curl", page!.Fetcher);
      Assert.True(page.HasBodyText);
      Assert.Null(page.FetchErrorMessage);
   }

   [Fact]
   public async Task FetchTriesBrowserFor404WithBlockedBody()
   {
      var browserCalls = 0;
      var blockedHtml = BlockedHtmlDocument();
      var client = CreateClient(
         new HttpClient(new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
               Content = new StringContent(blockedHtml)
            }
         )),
         (url, strategies, token) =>
         {
            browserCalls++;
            return Task.FromResult(
               BrowserRender("Report", RichBodyHtml())
            );
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/blocked-404",
         CancellationToken.None
      );

      Assert.Equal(1, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("playwright", page!.Fetcher);
   }

   [Fact]
   public async Task FetchReturnsBlockedFailureWhenAllStagesBlocked()
   {
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(BlockedHtmlDocument())),
         BrowserNoRenderFunc,
         (url, maxTime, token) =>
         {
            return Task.FromResult(CurlHtmlResponse(
               url,
               200,
               BlockedHtmlDocument()
            ));
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/blocked",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal(
         WebPageFetchErrorKind.BrowserBlocked,
         page!.FetchErrorKind
      );
      Assert.False(page.HasBodyText);
      Assert.Contains("block or challenge",
         page.FetchErrorMessage,
         StringComparison.OrdinalIgnoreCase
      );
   }

   [Fact]
   public async Task FetchSoftNotFoundOn2xxIsTerminal()
   {
      var curlCalls = 0;
      var html =
         """
         <html>
            <head><title>Official site</title></head>
            <body><main>
               <h1>Let!</h1>
               <p>This page does not exist.</p>
            </main></body>
         </html>
         """;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(html)),
         BrowserNoRenderFunc,
         (url, maxTime, token) =>
         {
            curlCalls++;
            return Task.FromResult(WebPageHttpResponse.Failure(
               url,
               "curl not available in test"
            ));
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/soft-not-found",
         CancellationToken.None
      );

      Assert.Equal(0, curlCalls);
      Assert.NotNull(page);
      Assert.Equal(
         WebPageFetchErrorKind.HttpError,
         page!.FetchErrorKind
      );
      Assert.False(page.HasBodyText);
      Assert.Contains("not found", page.FetchErrorMessage,
         StringComparison.OrdinalIgnoreCase
      );
   }

   [Fact]
   public async Task FetchIgnoresSoftErrorTextInEmbeddedState()
   {
      var html =
         """
         <html>
            <head><title>Live schedule</title></head>
            <body>
               <main>
                  <h1>Live schedule</h1>
                  <p>Useful schedule data.</p>
               </main>
               <script type="application/json">
                  {
                     "translations": {
                        "error": {
                           "404": "This page does not exist."
                        },
                        "refresh":
                           "Last refreshed {{time}} seconds ago."
                     }
                  }
               </script>
            </body>
         </html>
         """;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(html)),
         BrowserNoRenderFunc,
         CurlFailure
      );

      var page = await client.FetchAsync(
         "https://example.test/embedded-error-translation",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Null(page!.FetchErrorKind);
      Assert.Contains("Useful schedule data.", page.MainText);
   }

   [Fact]
   public async Task FetchAppendsTextExtractedFromRelevantImages()
   {
      var ocrCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(ShortHtmlDocument())),
         (url, strategies, token) =>
         {
            return Task.FromResult(BrowserRender(
               "Report",
               "<main><p>Rendered body text from the browser page.</p></main>",
               images:
                  [
                     new WebPageImageCandidate(
                        "https://example.test/document.png",
                        1200,
                        800,
                        null
                     )
                  ]
            ));
         },
         imageTextFetcher: (images, token) =>
         {
            ocrCalls++;
            return Task.FromResult("OCR extracted text.");
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/with-image",
         CancellationToken.None
      );

      Assert.Equal(1, ocrCalls);
      Assert.NotNull(page);
      Assert.Contains(
         "Rendered body text from the browser page.",
         page!.MainText
      );
      Assert.Contains("OCR extracted text.", page.MainText);
   }

   [Fact]
   public async Task FetchSkipsOcrWhenTextIsAlreadyRich()
   {
      var ocrCalls = 0;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(RichHtmlDocument())),
         BrowserNoRenderFunc,
         CurlFailure,
         (images, token) =>
         {
            ocrCalls++;
            return Task.FromResult("OCR extracted text.");
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/rich-with-image",
         CancellationToken.None
      );

      Assert.Equal(0, ocrCalls);
      Assert.NotNull(page);
      Assert.Equal("http", page!.Fetcher);
   }

   [Fact]
   public async Task FetchExtractsTextFromPdfResponses()
   {
      var browserCalls = 0;
      var handler = new PdfRecordingHandler(CreatePdfBytes());
      var client = CreateClient(
         new HttpClient(handler),
         (url, strategies, token) =>
         {
            browserCalls++;
            return Task.FromResult(NoRenderOutcome());
         }
      );

      var page = await client.FetchAsync(
         "https://example.test/entry-list.pdf",
         CancellationToken.None
      );

      Assert.Equal(0, browserCalls);
      Assert.NotNull(page);
      Assert.Equal("http", page!.Fetcher);
      Assert.Equal("Sample PDF Title", page.Title);
      Assert.Contains("Hello from the PDF body.", page.MainText);
   }

   [Fact]
   public async Task FetchPreservesPdfLineBreaks()
   {
      var handler = new PdfRecordingHandler(CreateMultiLinePdfBytes());
      var client = CreateClient(new HttpClient(handler));

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
   public async Task FetchKeepsPdfTableRowsHorizontallyAligned()
   {
      var handler = new PdfRecordingHandler(CreateAlignedPdfBytes());
      var client = CreateClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/aligned.pdf",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal(
         "GUS GREENSMITH | GBR\nJonas ANDERSSON | SWE",
         page!.MainTextFull.ReplaceLineEndings("\n").Trim()
      );
   }

   [Fact]
   public async Task FetchKeepsTightlySpacedPdfCellLinesSeparate()
   {
      var handler = new PdfRecordingHandler(
         CreateTightlySpacedDriverPdfBytes()
      );
      var client = CreateClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/drivers.pdf",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal(
         "(B) Daniel Goldburg | USA\n" +
         "(P) Paul Di Resta | GBR\n" +
         "(S) Rasmus Lindh | SWE",
         page!.MainTextFull.ReplaceLineEndings("\n").Trim()
      );
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
               Content = new StringContent(RichHtmlDocument())
            };
         }))
      );

      await client.FetchAsync(
         "https://example.test/article",
         CancellationToken.None
      );

      Assert.NotNull(seenRequest);
      Assert.Contains(
         seenRequest!.Headers.Accept,
         value => string.Equals(
            value.MediaType,
            "text/html",
            StringComparison.OrdinalIgnoreCase
         )
      );
      Assert.True(
         seenRequest.Headers.TryGetValues(
            "Accept-Language",
            out var languages
         )
      );
      Assert.Contains("en-US", languages);
      Assert.Contains(
         TestBrowserUserAgent,
         seenRequest.Headers.UserAgent.ToString()
      );
   }

   [Fact]
   public async Task FetchPassesUrlAndStrategiesToBrowser()
   {
      Uri? seenUrl = null;
      IReadOnlyList<BrowserStrategyDescriptor>? seenStrategies = null;
      var client = CreateClient(
         new HttpClient(new HtmlRecordingHandler(ShortHtmlDocument())),
         (url, strategies, token) =>
         {
            seenUrl = url;
            seenStrategies = strategies;
            return Task.FromResult(NoRenderOutcome());
         }
      );

      await client.FetchAsync(
         "https://example.test/article",
         CancellationToken.None
      );

      Assert.NotNull(seenUrl);
      Assert.Equal("https://example.test/article", seenUrl!.ToString());
      Assert.NotNull(seenStrategies);
      Assert.NotEmpty(seenStrategies);
   }

   [Fact]
   public async Task RedirectToPublicTargetIsFollowed()
   {
      var client = CreateClient(
         new HttpClient(new RecordingHandler(request =>
         {
            if(request.RequestUri!.AbsolutePath == "/start")
            {
               var response = new HttpResponseMessage(
                  HttpStatusCode.Redirect
               );
               response.Headers.Location =
                  new Uri("https://example.test/target");
               return response;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
               Content = new StringContent(RichHtmlDocument())
            };
         }))
      );

      var page = await client.FetchAsync(
         "https://example.test/start",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal("http", page!.Fetcher);
      Assert.True(page.HasBodyText);
   }

   [Fact]
   public async Task RedirectToBlockedTargetFails()
   {
      var client = CreateClient(
         new HttpClient(new RecordingHandler(request =>
         {
            var response = new HttpResponseMessage(
               HttpStatusCode.Redirect
            );
            response.Headers.Location =
               new Uri("http://169.254.169.254/metadata");
            return response;
         }))
      );

      var page = await client.FetchAsync(
         "https://example.test/redirects-away",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal(
         WebPageFetchErrorKind.HttpError,
         page!.FetchErrorKind
      );
      Assert.Contains("Redirect target rejected",
         page.FetchErrorMessage
      );
   }

   [Fact]
   public async Task DirectTransportRejectsOversizedResponse()
   {
      var client = new HttpClient(new RecordingHandler(_ =>
      {
         var response = new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new ByteArrayContent([])
         };
         response.Content.Headers.ContentLength =
            (long)WebPageFetchDefaults.MaximumResponseBytes + 1;
         return response;
      }));

      var response = await WebPageHttpTransport.SendAsync(
         client,
         new Uri("https://example.test/large"),
         TestBrowserUserAgent,
         CancellationToken.None
      );

      Assert.Equal(
         WebPageFetchErrorKind.ResponseTooLarge,
         response.ErrorKind
      );
      Assert.Equal(200, response.StatusCode);
   }

   [Fact]
   public async Task RedirectLoopFails()
   {
      var response = await WebPageHttpTransport.SendAsync(
         new HttpClient(new RecordingHandler(request =>
         {
            var redirect = new HttpResponseMessage(
               HttpStatusCode.Redirect
            );
            redirect.Headers.Location = request.RequestUri;
            return redirect;
         })),
         new Uri("https://example.test/loop"),
         TestBrowserUserAgent,
         CancellationToken.None
      );

      Assert.NotNull(response.TransportError);
      Assert.Contains("Redirect loop detected", response.TransportError);
   }

   [Fact]
   public async Task HtmlCandidateExtractionFeedsAssessor()
   {
      var html =
         """
         <html>
            <head><title>Bracket</title></head>
            <body>
               <header><h2>Site navigation</h2></header>
               <main>
                  <h1>Bracket</h1>
                  <h2>Round one</h2>
                  <p>TBD TBD TBD</p>
               </main>
            </body>
         </html>
         """;
      var candidate = WebPageHtmlCandidate.FromHtml(
         html,
         new Uri("https://example.test/bracket")
      );

      Assert.Equal(
         ["H1: Bracket", "H2: Round one"],
         candidate.Headings
      );
      Assert.Contains("TBD", candidate.RenderWarning);

      var assessment = candidate.Assess(WebPageBlockSource.HtmlFallback);
      Assert.Equal(
         WebPageContentClassification.NeedsRendering,
         assessment.Classification
      );
   }

   [Fact]
   public void AssessorClassifiesEmptyContent()
   {
      var assessment = WebPageContentAssessor.Assess(
         "Title",
         string.Empty,
         string.Empty,
         null,
         0,
         false,
         0,
         WebPageBlockSource.HtmlFallback
      );

      Assert.Equal(
         WebPageContentClassification.Empty,
         assessment.Classification
      );
   }

   [Fact]
   public void AssessorClassifiesSoftNotFound()
   {
      var assessment = WebPageContentAssessor.Assess(
         "Official site",
         "Let! This page does not exist.",
         "Let! This page does not exist.",
         null,
         1,
         false,
         0,
         WebPageBlockSource.HtmlFallback
      );

      Assert.Equal(
         WebPageContentClassification.NotFound,
         assessment.Classification
      );
      Assert.NotNull(assessment.SoftNotFoundSignature);
   }

   [Fact]
   public void AssessorClassifiesBlockedContent()
   {
      var html = BlockedHtmlDocument();
      var candidate = WebPageHtmlCandidate.FromHtml(
         html,
         new Uri("https://example.test/blocked")
      );
      var assessment = candidate.Assess(WebPageBlockSource.HtmlFallback);

      Assert.Equal(
         WebPageContentClassification.Blocked,
         assessment.Classification
      );
      Assert.NotNull(assessment.BlockSignature);
   }

   [Fact]
   public void AssessorClassifiesRichContentAsUsable()
   {
      var candidate = WebPageHtmlCandidate.FromHtml(
         RichHtmlDocument(),
         new Uri("https://example.test/rich")
      );
      var assessment = candidate.Assess(WebPageBlockSource.HtmlFallback);

      Assert.True(assessment.IsSuccess);
   }

   [Fact]
   public void AssessorClassifiesShortUnstructuredContentAsPartial()
   {
      var candidate = WebPageHtmlCandidate.FromHtml(
         ShortHtmlDocument(),
         new Uri("https://example.test/short")
      );
      var assessment = candidate.Assess(WebPageBlockSource.HtmlFallback);

      Assert.Equal(
         WebPageContentClassification.Partial,
         assessment.Classification
      );
   }

   [Fact]
   public void BrowserPolicyPrefersSuccessfulStrategyForOrigin()
   {
      var policy = new BrowserStrategyPolicy();
      var url = new Uri("https://policy-preference.test/page");

      policy.ReportSuccess(url, "firefox-bundled");

      var strategies = policy.GetStrategies(url);

      Assert.Equal("firefox-bundled", strategies[0].Id);
   }

   [Fact]
   public void BrowserPolicyKeepsDefaultOrderWithoutPreference()
   {
      var policy = new BrowserStrategyPolicy();
      var url = new Uri("https://policy-default.test/page");

      var strategies = policy.GetStrategies(url);

      Assert.Equal(
         BrowserStrategyDescriptor.All.Select(s => s.Id),
         strategies.Select(s => s.Id)
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
      Assert.True(page!.HasBodyText);
   }

   // ---- Fake evidence builders ------------------------------------------

   private static WebPageContentClient CreateClient(
      HttpClient httpClient,
      Func<Uri, IReadOnlyList<BrowserStrategyDescriptor>,
         CancellationToken, Task<WebPageBrowserOutcome>>? browserFetcher = null,
      Func<Uri, int, CancellationToken,
         Task<WebPageHttpResponse>>? curlTransport = null,
      Func<IReadOnlyList<WebPageImageCandidate>,
         CancellationToken, Task<string>>? imageTextFetcher = null
   )
   {
      return new WebPageContentClient(
         httpClient,
         browserFetcher ?? BrowserNoRenderFunc,
         NullLogger<WebPageContentClient>.Instance,
         () => Task.FromResult(TestBrowserUserAgent),
         curlTransport ?? CurlFailure,
         imageTextFetcher ?? ((images, token) => Task.FromResult(""))
      );
   }

   private static Task<WebPageBrowserOutcome> BrowserNoRenderFunc(
      Uri url,
      IReadOnlyList<BrowserStrategyDescriptor> strategies,
      CancellationToken token
   )
   {
      return Task.FromResult(
         NoRenderOutcome()
      );
   }

   private static WebPageBrowserOutcome NoRenderOutcome()
   {
      // Launched but did not render: this is a navigation failure, not a
      // launch failure, so it must not poison the process-wide launch
      // cooldown state shared by other tests.
      return new WebPageBrowserOutcome(
         null,
         [
            new WebPageBrowserStrategyAttempt(
               "chromium-bundled",
               true,
               false,
               null,
               "navigation timed out",
               WebPageFetchErrorKind.Timeout
            )
         ]
      );
   }

   private static WebPageBrowserOutcome BrowserRender(
      string title,
      string bodyHtml,
      int status = 200,
      IReadOnlyList<WebPageImageCandidate>? images = null
   )
   {
      var fullHtml =
         $"<html><head><title>{title}</title></head>" +
         $"<body>{bodyHtml}</body></html>";
      var render = new WebPageBrowserRenderResult(
         fullHtml,
         bodyHtml,
         title,
         images ?? [],
         status,
         "chromium-bundled"
      );

      return new WebPageBrowserOutcome(
         render,
         [
            new WebPageBrowserStrategyAttempt(
               "chromium-bundled",
               true,
               true,
               status,
               null,
               null
            )
         ]
      );
   }

   private static Task<WebPageHttpResponse> CurlFailure(
      Uri url,
      int maxTime,
      CancellationToken token
   )
   {
      return Task.FromResult(WebPageHttpResponse.Failure(
         url,
         "curl not available in test"
      ));
   }

   private static WebPageHttpResponse CurlHtmlResponse(
      Uri url,
      int status,
      string html
   )
   {
      return new WebPageHttpResponse(
         url,
         url,
         false,
         status,
         "text/html; charset=utf-8",
         Encoding.UTF8.GetBytes(html),
         null,
         null
      );
   }

   private static string ShortHtmlDocument()
   {
      return """
         <html>
            <head><title>Short</title></head>
            <body><main><p>Short body text.</p></main></body>
         </html>
         """;
   }

   private static string RichBodyHtml()
   {
      var sentence =
         "The round was covered in detail, with timing splits, " +
         "position changes and commentary throughout the session.";

      return "<main><h1>Report</h1>" +
         string.Join(
            "",
            Enumerable.Range(0, 20).Select(index => $"<p>{sentence}</p>")
         ) +
         "</main>";
   }

   private static string RichHtmlDocument()
   {
      return "<html><head><title>Report</title></head>" +
         $"<body>{RichBodyHtml()}</body></html>";
   }

   private static string BlockedHtmlDocument()
   {
      return """
         <html>
            <head><title>Just a moment...</title></head>
            <body>Performing security verification.</body>
         </html>
         """;
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

   // ---- Fake handlers -----------------------------------------------------

   private sealed class GateHandler : HttpMessageHandler
   {
      private readonly Task gate;

      public GateHandler(Task gate)
      {
         this.gate = gate;
      }

      protected override async Task<HttpResponseMessage>
         SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
         )
      {
         using var source = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
         source.CancelAfter(TimeSpan.FromSeconds(5));

         try
         {
            await gate.WaitAsync(source.Token);
         }
         catch(OperationCanceledException) when
            (cancellationToken.IsCancellationRequested)
         {
            throw;
         }

         return new HttpResponseMessage(HttpStatusCode.OK);
      }
   }

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly Func<HttpRequestMessage, HttpResponseMessage>
         respond;

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

   private sealed class HtmlRecordingHandler : HttpMessageHandler
   {
      private readonly string content;

      public HtmlRecordingHandler(string content)
      {
         this.content = content;
      }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new StringContent(content)
         });
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
         return Task.FromException<HttpResponseMessage>(exception);
      }
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

   // ---- PDF byte builders (copied from the legacy test file) --------------

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

   private static byte[] CreateAlignedPdfBytes()
   {
      var builder = new PdfDocumentBuilder();
      var page = builder.AddPage(PageSize.A4);
      var font = builder.AddStandard14Font(Standard14Font.Helvetica);
      builder.DocumentInformation.Title = "Aligned PDF";
      page.AddText("GUS GREENSMITH", 12, new PdfPoint(72, 720), font);
      page.AddText("Jonas ANDERSSON", 12, new PdfPoint(72, 700), font);
      page.AddText("GBR", 12, new PdfPoint(240, 720), font);
      page.AddText("SWE", 12, new PdfPoint(240, 700), font);
      return builder.Build();
   }

   private static byte[] CreateTightlySpacedDriverPdfBytes()
   {
      var builder = new PdfDocumentBuilder();
      var page = builder.AddPage(PageSize.A4);
      var font = builder.AddStandard14Font(Standard14Font.Helvetica);
      builder.DocumentInformation.Title = "Driver PDF";

      page.AddText(
         "(B) Daniel Goldburg",
         12,
         new PdfPoint(72, 720),
         font
      );
      page.AddText("USA", 12, new PdfPoint(240, 720), font);
      page.AddText(
         "(P) Paul Di Resta",
         12,
         new PdfPoint(72, 708),
         font
      );
      page.AddText("GBR", 12, new PdfPoint(240, 708), font);
      page.AddText(
         "(S) Rasmus Lindh",
         12,
         new PdfPoint(72, 696),
         font
      );
      page.AddText("SWE", 12, new PdfPoint(240, 696), font);

      return builder.Build();
   }

   // Real-curl integration tests. These shell out to the system curl
   // against a loopback HTTP server, which is the only way to catch
   // write-out format and parser drift (multiple --write-out options are
   // not concatenated by curl; the last one wins).

   [Fact]
   public async Task CurlTransportFetchesThroughRealCurl()
   {
      var server = new LoopbackHttpServer();
      server.Responses["/"] = new LoopbackResponse(
         "200 OK",
         ["Content-Type: text/html; charset=utf-8", "X-Test: curl"],
         "hello curl"
      );

      try
      {
         var response = await WebPageCurlTransport.SendAsync(
            server.UriFor("/"),
            10,
            CancellationToken.None
         );

         Assert.Null(response.TransportError);
         Assert.Equal(200, response.StatusCode);
         Assert.Contains("text/html", response.ContentType);
         Assert.Equal("hello curl", Encoding.UTF8.GetString(response.Body));
      }
      finally
      {
         server.Dispose();
      }
   }

   // A redirect to a non-public target must be rejected by the URL
   // policy. This also proves the real-curl header path end to end: the
   // Location header can only be rejected if it was parsed from curl's
   // %{header_json} in the first place.
   [Fact]
   public async Task CurlTransportRejectsRedirectToNonPublicTarget()
   {
      var server = new LoopbackHttpServer();
      server.Responses["/start"] = new LoopbackResponse(
         "302 Found",
         [
            "Location: http://169.254.169.254/latest/meta-data/"
         ],
         ""
      );

      try
      {
         var response = await WebPageCurlTransport.SendAsync(
            server.UriFor("/start"),
            10,
            CancellationToken.None
         );

         Assert.Null(response.TransportError);
         Assert.Null(response.StatusCode);
         Assert.NotNull(response.RedirectPolicyError);
      }
      finally
      {
         server.Dispose();
      }
   }

   private sealed class LoopbackHttpServer : IDisposable
   {
      private readonly TcpListener _listener;

      internal LoopbackHttpServer()
      {
         _listener = new TcpListener(IPAddress.Loopback, 0);
         _listener.Start();
         Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
         _ = Task.Run(AcceptLoopAsync);
      }

      internal int Port { get; }

      internal Dictionary<string, LoopbackResponse> Responses { get; } =
         new();

      internal Uri UriFor(string path)
      {
         return new Uri($"http://127.0.0.1:{Port}{path}");
      }

      private async Task AcceptLoopAsync()
      {
         while(true)
         {
            TcpClient client;
            try
            {
               client = await _listener.AcceptTcpClientAsync();
            }
            catch(ObjectDisposedException)
            {
               return;
            }

            _ = Task.Run(() => HandleAsync(client));
         }
      }

      private async Task HandleAsync(TcpClient client)
      {
         try
         {
            using var connection = client;
            var stream = connection.GetStream();
            var request = new List<byte>();
            var buffer = new byte[4096];

            while(!Encoding.ASCII.GetString(request.ToArray())
               .Contains("\r\n\r\n"))
            {
               var read = await stream.ReadAsync(buffer);
               if(read == 0)
               {
                  return;
               }

               request.AddRange(buffer.Take(read));
            }

            var path = Encoding.ASCII
               .GetString(request.ToArray())
               .Split('\r', '\n')[0]
               .Split(' ')[1];

            if(!Responses.TryGetValue(path, out var response))
            {
               await WriteRawAsync(
                  stream,
                  "HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\n\r\n"
               );
               return;
            }

            var body = Encoding.UTF8.GetBytes(response.Body);
            var headerBlock = string.Join("\r\n", response.Headers);
            await WriteRawAsync(
               stream,
               $"HTTP/1.1 {response.StatusLine}\r\n{headerBlock}\r\n" +
                  $"Content-Length: {body.Length}\r\n\r\n"
            );
            await stream.WriteAsync(body);
         }
         catch(Exception)
         {
            // Curl closes connections as it finishes; teardown errors are
            // expected and must not fail the test run.
         }
      }

      private static async Task WriteRawAsync(
         NetworkStream stream,
         string raw
      )
      {
         await stream.WriteAsync(Encoding.ASCII.GetBytes(raw));
         await stream.FlushAsync();
      }

      public void Dispose()
      {
         _listener.Stop();
      }
   }

   private sealed record LoopbackResponse(
      string StatusLine,
      IReadOnlyList<string> Headers,
      string Body
   );
}
