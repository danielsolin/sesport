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
      Assert.Contains("Heading", page.MainText);
      Assert.Contains("First paragraph.", page.MainText);
      Assert.Contains("Second paragraph.", page.MainText);
      Assert.DoesNotContain("Menu item", page.MainText);
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

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly string html;

      public RecordingHandler(string html)
      {
         this.html = html;
      }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
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
}
