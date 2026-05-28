using SESport.Sources.Iihf;

namespace SESport.Core.Tests;

public class IihfScheduleHtmlParserTests
{
   [Fact]
   public void ParserCanReadSwitzerlandVsSwedenFromScheduleHtml()
   {
      var parser = new IihfScheduleHtmlParser();

      var games = parser.Parse(ScheduleHtml);

      var game = games.Single();

      Assert.Equal("SUI vs SWE", $"{game.HomeTeam.ExternalId[..3].ToUpper()} " +
         $"vs {game.AwayTeam.ExternalId[..3].ToUpper()}");
      Assert.Equal(
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         game.StartsAt
      );
      Assert.Equal("Switzerland", game.HomeTeam.CountryName);
      Assert.Equal("Sweden", game.AwayTeam.CountryName);
   }

   private const string ScheduleHtml = """
      <html>
         <body>
            <section>
               <h2>28 May</h2>
               <div>
                  <span>SUI</span>
                  <span>SWE</span>
                  <strong>SUI vs SWE</strong>
                  <span>Swiss Life Arena</span>
                  <time>20:20</time>
               </div>
            </section>
         </body>
      </html>
      """;
}
