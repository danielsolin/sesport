using System.Net;

using SESport.AI.Providers;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace SESport.Core.Tests.AI;

public class WebPageContentClientTests
{
   [Fact]
   public async Task FetchReturnsBrowserContent()
   {
      var browserCalls = 0;
      var client = new WebPageContentClient(
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
      var client = new WebPageContentClient(
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
      var client = new WebPageContentClient(
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
   public async Task FetchExtractsTextFromPdfResponses()
   {
      var browserCalls = 0;
      var handler = new PdfRecordingHandler(CreatePdfBytes());
      var client = new WebPageContentClient(
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
   public async Task FetchPreservesPdfLineBreaks()
   {
      var handler = new PdfRecordingHandler(CreateMultiLinePdfBytes());
      var client = new WebPageContentClient(
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
      var client = new WebPageContentClient(
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
      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         var response = new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new StringContent("<html></html>")
         };
         response.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
               "text/html"
            );

         return Task.FromResult(response);
      }
   }
}
