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

   [Fact]
   public void ParserCanReadSwitzerlandVsSwedenFromStatsTable()
   {
      var parser = new IihfScheduleHtmlParser();

      var games = parser.Parse(StatsTableHtml);

      var game = games.Single();

      Assert.Equal("iihf-2026-game-59", game.ExternalId);
      Assert.Equal("Quarter-final", game.Stage);
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

   private const string StatsTableHtml = """
      <html>
         <body>
            <table id="gameReports">
               <tr>
                  <td class="even" id="gdt59" utc="29666540" tzo="120">
                     <nobr>&nbsp;28 May 2026, Thu<br>
                     &nbsp;20:20&nbsp;&nbsp;GMT+2</nobr>
                  </td>
                  <td width="1" class="even2"></td>
                  <td class="even">
                     <nobr>&nbsp;Zurich</nobr><br>
                     <nobr>&nbsp;Swiss Life Arena</nobr>
                  </td>
                  <td width="1" class="even2"></td>
                  <td class="even" align="right">
                     &nbsp;<nobr>59</nobr><br>
                     <b>QF</b>
                  </td>
                  <td width="1" class="even2"></td>
                  <td class="even" align="right">
                     <b>SUI</b>&nbsp;
                  </td>
                  <td class="even">-</td>
                  <td class="even">&nbsp;<b>SWE</b></td>
                  <td width="1" class="even2"></td>
                  <td class="even" align="center"></td>
                  <td width="1" class="even2"></td>
                  <td class="even"> Scheduled </td>
               </tr>
            </table>
         </body>
      </html>
      """;
}
