using System.Net;
using System.Text;
using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public class DuckDuckGoWebSearchClientTests
{
   [Fact]
   public async Task SearchParsesDuckDuckGoHtmlResults()
   {
      var handler = new RecordingHandler(CreateHtml());
      var client = new DuckDuckGoWebSearchClient(new HttpClient(handler));

      var results = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Contains(
         "https://html.duckduckgo.com/html/?q=",
         handler.RequestUri?.ToString()
      );
      Assert.Contains("Tre", handler.RequestUri?.ToString());
      Assert.Single(results);
      Assert.Equal("Tre Kronor roster", results[0].Title);
      Assert.Equal("https://example.test/article", results[0].Url);
      Assert.Equal("Sweden lineup info.", results[0].Snippet);
   }

   private static string CreateHtml()
   {
      const string encodedUrl =
         "https%3A%2F%2Fexample.test%2Farticle";

      return string.Join(
         Environment.NewLine,
         [
            "<html>",
            "   <body>",
            "      <div class=\"result results_links\">",
            "         <div>",
            "            <a class=\"result__a\" href=\"" +
               "//duckduckgo.com/l/?uddg=" +
               encodedUrl + "\">",
            "               Tre Kronor roster",
            "            </a>",
            "            <a class=\"result__snippet\">",
            "               Sweden lineup info.",
            "            </a>",
            "         </div>",
            "      </div>",
            "   </body>",
            "</html>"
         ]
      );
   }

   private sealed class RecordingHandler(
      string responseText
   ) : HttpMessageHandler
   {
      public Uri? RequestUri { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestUri = request.RequestUri;

         return Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
               Content = new StringContent(
                  responseText,
                  Encoding.UTF8,
                  "text/html"
               )
            }
         );
      }
   }
}
