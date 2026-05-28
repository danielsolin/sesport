using SESport.Sources.Iihf;

namespace SESport.Core.Tests;

public class FileIihfScheduleClientTests
{
   [Fact]
   public async Task ClientReadsSavedScheduleHtmlAndReturnsMatchingGames()
   {
      var filePath = Path.Combine(
         Path.GetTempPath(),
         $"sesport-iihf-schedule-{Guid.NewGuid():N}.html"
      );

      try
      {
         await File.WriteAllTextAsync(filePath, ScheduleHtml);

         var client = new FileIihfScheduleClient(
            filePath,
            new IihfScheduleHtmlParser()
         );
         var request = new ImportRequest(
            new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.FromHours(2))
         );

         var games = await client.GetGamesAsync(
            request,
            CancellationToken.None
         );

         Assert.Single(games);
         Assert.Equal(
            "SUI",
            games.Single().HomeTeam.ExternalId[..3].ToUpper()
         );
         Assert.Equal(
            "SWE",
            games.Single().AwayTeam.ExternalId[..3].ToUpper()
         );
      }
      finally
      {
         if (File.Exists(filePath))
         {
            File.Delete(filePath);
         }
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
