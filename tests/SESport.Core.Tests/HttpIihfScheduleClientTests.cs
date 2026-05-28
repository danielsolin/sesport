using System.Net;
using SESport.Sources.Iihf;

namespace SESport.Core.Tests;

public class HttpIihfScheduleClientTests
{
   [Fact]
   public async Task ClientFetchesScheduleHtmlAndReturnsMatchingGames()
   {
      var handler = new StubHttpMessageHandler(ScheduleHtml);
      var client = new HttpIihfScheduleClient(
         new HttpClient(handler),
         new IihfScheduleHtmlParser(),
         new Uri("https://www.iihf.com/en/events/2026/wm/schedule")
      );
      var request = new ImportRequest(
         new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.FromHours(2)),
         new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.FromHours(2))
      );

      var games = await client.GetGamesAsync(request, CancellationToken.None);

      Assert.Single(games);
      Assert.Equal("SUI", games.Single().HomeTeam.ExternalId[..3].ToUpper());
      Assert.Equal("SWE", games.Single().AwayTeam.ExternalId[..3].ToUpper());
   }

   private sealed class StubHttpMessageHandler(
      string responseBody
   ) : HttpMessageHandler
   {
      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         var response = new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new StringContent(responseBody)
         };

         return Task.FromResult(response);
      }
   }

   private const string ScheduleHtml = """
      <html>
         <body>
            <p>28 May</p>
            <p>SUI vs SWE</p>
            <p>Swiss Life Arena, Quarterfinals</p>
            <p>20:20</p>
         </body>
      </html>
      """;
}
