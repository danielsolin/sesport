using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public class WebPageContentClientTests
{
   [Fact]
   public async Task FetchExtractsMainTextAndDateFromHtml()
   {
      var handler = new RecordingHandler(CreateHtml());
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/article",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Equal("Example Title", page!.Title);
      Assert.Equal("https://example.test/article", page.Url);
      Assert.Equal(
         DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
         page.PublishedAt
      );
      Assert.Contains("Heading", page.Headings);
      Assert.Contains("Heading", page.MainText);
      Assert.Contains("First paragraph.", page.MainText);
      Assert.Contains("Second paragraph.", page.MainText);
      Assert.DoesNotContain("Menu item", page.MainText);
      Assert.Contains("Mozilla/5.0", handler.UserAgentHeader);
      Assert.Equal("en-US,en;q=0.9", handler.AcceptLanguageHeader);
   }

   [Fact]
   public async Task FetchSkipsNoisyHeadingsWithFormMarkup()
   {
      var handler = new RecordingHandler(CreateNoisyHeadingHtml());
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/noisy-heading",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.DoesNotContain("Calendar", page!.Headings);
      Assert.DoesNotContain("2026/2027", page.Headings);
   }

   [Fact]
   public async Task FetchFallsBackToEmbeddedJsonWhenBodyIsEmpty()
   {
      var handler = new RecordingHandler(CreateClientRenderedHtml());
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/client-rendered",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains("client-rendered", page!.MainText);
      Assert.Contains("Site settings", page.MainText);
      Assert.Contains("webAPIBaseURL", page.MainText);
      Assert.Contains("api-web.nhle.com", page.MainText);
      Assert.Contains("Example description.", page.MainText);
   }

   [Fact]
   public async Task FetchPrefersEmbeddedJsonWhenBodyIsMostlyNoise()
   {
      var handler = new RecordingHandler(CreateMostlyNoisyJsonHtml());
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/noisy-json",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains("Ingrid Lindblad", page!.MainText);
      Assert.Contains("SWE", page.MainText);
      Assert.DoesNotContain("0PX 0PX 0PX", page.MainText);
   }

   [Fact]
   public async Task FetchSkipsPdfResponses()
   {
      var handler = new PdfRecordingHandler();
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/entry-list.pdf",
         CancellationToken.None
      );

      Assert.Null(page);
   }

   [Fact]
   public async Task FetchTranslatesFlagClassesToCountryNames()
   {
      var handler = new RecordingHandler(CreateFlagHtml());
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/flags",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains("Sweden", page!.MainText);
      Assert.Contains("Example Player", page.MainText);
      Assert.DoesNotContain("Detected flags:", page.MainText);
   }

   [Fact]
   public async Task FetchTranslatesCommonCountryAttributes()
   {
      var handler = new RecordingHandler(CreateCountryAttributeHtml());
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/countries",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains("Sweden", page!.MainText);
      Assert.Contains("Norway", page.MainText);
      Assert.Contains("Finland", page.MainText);
   }

   [Fact]
   public async Task FetchFiltersCssLikeNoiseAndKeepsTableText()
   {
      var handler = new RecordingHandler(CreateNoisyTableHtml());
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/noisy-table",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.DoesNotContain("0PX 0PX 0PX", page!.MainText);
      Assert.Contains("Example Player", page.MainText);
      Assert.Contains("Entered", page.MainText);
   }

   [Fact]
   public async Task FetchFiltersDenseLayoutNoiseBeforeTableText()
   {
      var handler = new RecordingHandler(CreateDenseLayoutNoiseHtml());
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/dense-noise",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.DoesNotContain("0PX 0PX 0PX 0PX", page!.MainText);
      Assert.Contains("Example Player", page.MainText);
      Assert.Contains("Entered", page.MainText);
   }

   [Fact]
   public async Task FetchFiltersLpgaStyleLayoutNoiseLines()
   {
      var handler = new RecordingHandler(CreateLpgaStyleNoiseHtml());
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/lpga-noise",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.DoesNotContain("0PX", page!.MainText);
      Assert.DoesNotContain("SKIP TO MAIN CONTENT", page.MainText);
      Assert.Contains("NO.", page.MainText);
      Assert.Contains("ATHLETE", page.MainText);
      Assert.Contains("Example Player", page.MainText);
   }

   [Fact]
   public async Task FetchFallsBackToSupplementalTextWhenMainIsNoisy()
   {
      var handler = new RecordingHandler(
         CreateNoisyLayoutWithEmbeddedJsonHtml()
      );
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/noisy-layout-with-json",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.DoesNotContain("0PX", page!.MainText);
      Assert.Contains("Ingrid Lindblad", page.MainText);
      Assert.Contains("SWE", page.MainText);
   }

   [Fact]
   public async Task FetchIgnoresInvalidRegexEscapesInScripts()
   {
      var handler = new RecordingHandler(
         CreateInvalidEscapeScriptHtml()
      );
      var client = new WebPageContentClient(new HttpClient(handler));

      var page = await client.FetchAsync(
         "https://example.test/invalid-escape",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains("Example Title", page!.Title);
      Assert.Contains("Example Player", page.MainText);
      Assert.DoesNotContain("\\W", page.MainText);
   }

   private static string CreateHtml()
   {
      return """
      <html>
         <head>
            <title>Example Title</title>
            <meta property="article:published_time"
                  content="2026-06-15T12:34:56Z" />
         </head>
         <body>
            <nav>Menu item</nav>
            <article>
               <h1>Heading</h1>
               <p>First paragraph.</p>
               <p>Second paragraph.</p>
            </article>
         </body>
      </html>
      """;
   }

   private static string CreateNoisyHeadingHtml()
   {
      return """
      <html>
         <body>
            <article>
               <h2>
                  Calendar
                  <form class="seasonmenu select autosubmit"
                        action="/res/index.asp" method="get">
                     <select name="season" id="season">
                        <option value="2027">2027/2028</option>
                        <option value="2026" selected="selected">
                           2026/2027
                        </option>
                     </select>
                  </form>
               </h2>
               <p>Example body text.</p>
            </article>
         </body>
      </html>
      """;
   }

   private static string CreateClientRenderedHtml()
   {
      return """
      <html>
         <head>
            <title>Client Rendered Example</title>
            <meta name="description"
                  content="Example description." />
         </head>
         <body>
            <div id="root"></div>
            <script>
               window.__SITE_SETTINGS__ = {
                  "webAPIBaseURL": "https://api-web.nhle.com",
                  "appName": "NHL"
               };
            </script>
         </body>
      </html>
      """;
   }

   private static string CreateMostlyNoisyJsonHtml()
   {
      return """
      <html>
         <head>
            <title>Noise Example</title>
         </head>
         <body>
            <nav>0PX 0PX PRE 0PX SKIP TO MAIN CONTENT</nav>
            <script>
               window.__INITIAL_STATE__ = {
                  "page": {
                     "athletes": [
                        {
                           "name": "Ingrid Lindblad",
                           "country": {
                              "label": "SWE"
                           }
                        }
                     ]
                  }
               };
            </script>
         </body>
      </html>
      """;
   }

   private static string CreateFlagHtml()
   {
      return """
      <html>
         <body>
            <article>
               <div class="participant">
                  <span class="flag se __text_mode_custom_bg__"></span>
                  <span class="name">Example Player</span>
               </div>
            </article>
         </body>
      </html>
      """;
   }

   private static string CreateCountryAttributeHtml()
   {
      return """
      <html>
         <body>
            <article>
               <span class="flag-icon flag-icon-se"></span>
               <span data-country="no" aria-label="Norway"></span>
               <img alt="Finland flag" title="Finland" src="/fi.svg" />
            </article>
         </body>
      </html>
      """;
   }

   private static string CreateNoisyTableHtml()
   {
      return """
      <html>
         <body>
            <div>0PX 0PX 0PX</div>
            <table>
               <tr>
                  <th>ATHLETE</th>
                  <th>ENTRY STATUS</th>
               </tr>
               <tr>
                  <td>Example Player</td>
                  <td>Entered</td>
               </tr>
            </table>
         </body>
      </html>
      """;
   }

   private static string CreateDenseLayoutNoiseHtml()
   {
      return """
      <html>
         <body>
            <div>0PX 0PX 0PX 0PX</div>
            <div>0PX 0PX 0PX 0PX 0PX</div>
            <div>0PX 0PX 0PX 0PX 0PX 0PX</div>
            <table>
               <tr>
                  <td>Example Player</td>
                  <td>Entered</td>
               </tr>
            </table>
         </body>
      </html>
      """;
   }

   private static string CreateLpgaStyleNoiseHtml()
   {
      return """
      <html>
         <body>
            <div>0PX 0PX PRE 0PX SKIP TO MAIN CONTENT</div>
            <div>0PX 0PX 0PX 0PX 0PX 0PX Tours</div>
            <table>
               <tr>
                  <th>NO.</th>
                  <th>ATHLETE</th>
               </tr>
               <tr>
                  <td>1</td>
                  <td>Example Player</td>
               </tr>
            </table>
         </body>
      </html>
      """;
   }

   private static string CreateNoisyLayoutWithEmbeddedJsonHtml()
   {
      return """
      <html>
         <head>
            <title>Noise Example</title>
         </head>
         <body>
            <div>0PX 0PX PRE 0PX SKIP TO MAIN CONTENT</div>
            <div>0PX 0PX 0PX 0PX 0PX 0PX Tours</div>
            <script>
               window.__INITIAL_STATE__ = {
                  "page": {
                     "athletes": [
                        {
                           "name": "Ingrid Lindblad",
                           "country": {
                              "label": "SWE"
                           }
                        }
                     ]
                  }
               };
            </script>
         </body>
      </html>
      """;
   }

   private static string CreateInvalidEscapeScriptHtml()
   {
      return """
      <html>
         <head>
            <title>Example Title</title>
         </head>
         <body>
            <article>
               <h1>Example Player</h1>
               <p>Example body text.</p>
            </article>
            <script>
               window.__INITIAL_STATE__ = {
                  "value": "\W"
               };
            </script>
         </body>
      </html>
      """;
   }

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly string html;

      public RecordingHandler(string html)
      {
         this.html = html;
      }

      public string? UserAgentHeader { get; private set; }

      public string? AcceptLanguageHeader { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         UserAgentHeader = request.Headers.UserAgent.ToString();
         AcceptLanguageHeader = request.Headers.AcceptLanguage.ToString();

         return Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
               Content = new StringContent(
                  html,
                  System.Text.Encoding.UTF8,
                  "text/html"
               )
            }
         );
      }
   }

   private sealed class PdfRecordingHandler : HttpMessageHandler
   {
      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         var response = new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new ByteArrayContent(
               new byte[] { 0x25, 0x50, 0x44, 0x46 }
            )
         };
         response.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
               "application/pdf"
            );

         return Task.FromResult(response);
      }
   }
}
