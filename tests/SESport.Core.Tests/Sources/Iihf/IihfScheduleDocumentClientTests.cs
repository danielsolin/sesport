using System.Net;
using SESport.Sources.Iihf;

namespace SESport.Core.Tests.Sources.Iihf;

public class IihfScheduleDocumentClientTests
{
   private static readonly Uri FakeScheduleUri =
      new("https://example.test/iihf/schedule");

   [Fact]
   public async Task ClientFetchesScheduleDocumentAndReturnsMatchingGames()
   {
      var handler = new StubHttpMessageHandler(CreateScheduleHtml());
      var client = new IihfScheduleDocumentClient(
         new HttpClient(handler),
         new IihfScheduleHtmlParser(),
         FakeScheduleUri
      );
      var request = new ImportRequest(
         new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.FromHours(2)),
         new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.FromHours(2))
      );

      var games = await client.GetGamesAsync(request, CancellationToken.None);
      var game = games.Single();

      Assert.Equal("SUI", game.HomeTeam!.ExternalId[..3].ToUpper());
      Assert.Equal("SWE", game.AwayTeam!.ExternalId[..3].ToUpper());
   }

   private static string CreateScheduleHtml()
   {
      return "<p>28 May</p><p>SUI vs SWE</p><p>20:20</p>";
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
}
