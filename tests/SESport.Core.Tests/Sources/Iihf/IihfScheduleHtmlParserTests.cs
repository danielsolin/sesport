using SESport.Sources.Iihf;

namespace SESport.Core.Tests.Sources.Iihf;

public class IihfScheduleHtmlParserTests
{
   [Fact]
   public void ParserCanReadSwitzerlandVsSwedenFromScheduleHtml()
   {
      var parser = new IihfScheduleHtmlParser();

      var games = parser.Parse(ScheduleHtml);

      var game = games.Single();

      Assert.Equal("SUI vs SWE", $"{game.HomeTeam!.ExternalId[..3].ToUpper()} " +
         $"vs {game.AwayTeam!.ExternalId[..3].ToUpper()}");
      Assert.Equal(
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         game.StartsAt
      );
      Assert.Equal("Switzerland", game.HomeTeam.CountryName);
      Assert.Equal("Sweden", game.AwayTeam.CountryName);
   }

   [Fact]
   public void ParserCanReadSwitzerlandVsSwedenFromStatsTable()
   {
      var parser = new IihfScheduleHtmlParser();

      var games = parser.Parse(CreateStatsTable("59", "QF", "SUI", "SWE"));

      var game = games.Single();

      Assert.Equal("iihf-2026-game-59", game.ExternalId);
      Assert.Equal("Quarter-final", game.Stage);
      Assert.Equal(
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         game.StartsAt
      );
      Assert.Equal("Switzerland", game.HomeTeam!.CountryName);
      Assert.Equal("Sweden", game.AwayTeam!.CountryName);
   }

   [Fact]
   public void ParserCanReadStatsTableGameWithOneUnresolvedTeam()
   {
      var parser = new IihfScheduleHtmlParser();

      var games = parser.Parse(CreateStatsTable("63", "GMG", "SWE", ""));

      var game = games.Single();

      Assert.Equal("iihf-2026-game-63", game.ExternalId);
      Assert.Equal("Gold medal game", game.Stage);
      Assert.Equal("Sweden", game.HomeTeam!.CountryName);
      Assert.Null(game.AwayTeam);
   }

   private static string CreateStatsTable(
      string gameNumber,
      string stage,
      string homeCode,
      string awayCode
   )
   {
      return $$"""
         <html>
            <body>
               <table id="gameReports">
                  <tr>
                     <td class="even" id="gdt{{gameNumber}}" tzo="120">
                        28 May 2026, Thu 20:20 GMT+2
                     </td>
                     <td class="even">Zurich</td>
                     <td class="even">{{gameNumber}} {{stage}}</td>
                     <td class="even">{{homeCode}}</td>
                     <td class="even">-</td>
                     <td class="even">{{awayCode}}</td>
                     <td class="even"></td>
                     <td class="even">Scheduled</td>
                  </tr>
               </table>
            </body>
         </html>
         """;
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
