using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace SESport.Sources.Iihf;

public sealed partial class IihfScheduleHtmlParser
{
   private const string StatsDateRegex =
      @"(?<date>\d{1,2} [A-Z][a-z]{2} \d{4}).*?(?<time>\d{2}:\d{2})";

   private const string CompetitionExternalId =
      "iihf-world-championship-2026";

   private const string CompetitionName =
      "2026 IIHF Ice Hockey World Championship";

   public IReadOnlyCollection<IihfGame> Parse(string html)
   {
      var statsGames = ParseStatsTable(html);

      if(statsGames.Count > 0)
      {
         return statsGames;
      }

      var lines = ExtractTextLines(html);
      var games = new List<IihfGame>();
      DateOnly? currentDate = null;

      for(var index = 0; index < lines.Count; index++)
      {
         var line = lines[index];

         if(TryParseDate(line, out var date))
         {
            currentDate = date;
            continue;
         }

         var match = MatchupPattern().Match(line);

         if(!match.Success || currentDate is null)
         {
            continue;
         }

         var homeCode = match.Groups["home"].Value;
         var awayCode = match.Groups["away"].Value;
         var time = FindNextTime(lines, index + 1);

         if(time is null)
         {
            continue;
         }

         games.Add(
            CreateGame(currentDate.Value, time.Value, homeCode, awayCode)
         );
      }

      return games;
   }

   private static IReadOnlyCollection<IihfGame> ParseStatsTable(string html)
   {
      var document = new HtmlDocument();

      document.LoadHtml(html);

      var rows = document.DocumentNode
         .SelectNodes("//table[@id='gameReports']//tr") ??
         Enumerable.Empty<HtmlNode>();

      return rows
         .Select(ParseStatsRow)
         .Where(game => game is not null)
         .Select(game => game!)
         .ToList();
   }

   private static IihfGame? ParseStatsRow(HtmlNode row)
   {
      var cells = row
         .Elements("td")
         .Where(IsGameDataCell)
         .ToList();

      if(cells.Count < 8 || cells[0].GetAttributeValue("id", "") == "")
      {
         return null;
      }

      var startsAt = ParseStatsDate(cells[0]);
      var stage = ParseStage(ExtractText(cells[2]));
      var homeCode = ExtractTeamCode(cells[3]);
      var awayCode = ExtractTeamCode(cells[5]);
      var gameNumber = ExtractGameNumber(ExtractText(cells[2]));

      if(
         startsAt is null ||
         stage is null ||
         gameNumber is null ||
         (homeCode is null && awayCode is null)
      )
      {
         return null;
      }

      return new IihfGame(
         $"iihf-2026-game-{gameNumber}",
         CompetitionExternalId,
         CompetitionName,
         startsAt.Value,
         stage,
         homeCode is not null ? CreateTeam(homeCode) : null,
         awayCode is not null ? CreateTeam(awayCode) : null
      );
   }

   private static bool IsGameDataCell(HtmlNode cell)
   {
      var cellClass = cell.GetAttributeValue("class", "");

      return cellClass is "even" or "odd";
   }

   private static DateTimeOffset? ParseStatsDate(HtmlNode dateCell)
   {
      var text = ExtractText(dateCell);
      var match = StatsDatePattern().Match(text);

      if(!match.Success)
      {
         return null;
      }

      var offset = TimeSpan.FromMinutes(
         dateCell.GetAttributeValue("tzo", 0)
      );
      var value = $"{match.Groups["date"].Value} " +
         match.Groups["time"].Value;

      if(!DateTime.TryParseExact(
         value,
         "d MMM yyyy HH:mm",
         CultureInfo.InvariantCulture,
         DateTimeStyles.None,
         out var dateTime
      ))
      {
         return null;
      }

      return new DateTimeOffset(dateTime, offset);
   }

   private static string? ParseStage(string gameInfo)
   {
      var match = StagePattern().Match(gameInfo);

      if(!match.Success)
      {
         return null;
      }

      return match.Groups["stage"].Value switch
      {
         "BMG" => "Bronze medal game",
         "GMG" => "Gold medal game",
         "PRE" => "Preliminary round",
         "QF" => "Quarter-final",
         "SF" => "Semi-final",
         var stage => stage
      };
   }

   private static string? ExtractTeamCode(HtmlNode cell)
   {
      var text = ExtractText(cell);
      var match = TeamCodePattern().Match(text);

      return match.Success ? match.Groups["code"].Value : null;
   }

   private static string? ExtractGameNumber(string gameInfo)
   {
      var match = GameNumberPattern().Match(gameInfo);

      return match.Success ? match.Groups["number"].Value : null;
   }

   private static string ExtractText(HtmlNode node)
   {
      var text = WebUtility.HtmlDecode(node.InnerText) ?? string.Empty;

      return WhitespacePattern().Replace(text, " ").Trim();
   }

   private static IReadOnlyList<string> ExtractTextLines(string html)
   {
      var document = new HtmlDocument();

      document.LoadHtml(html);

      return document.DocumentNode
         .DescendantsAndSelf()
         .Where(node => node.NodeType == HtmlNodeType.Text)
         .Select(node => WebUtility.HtmlDecode(node.InnerText) ?? string.Empty)
         .Select(line => WhitespacePattern().Replace(line, " ").Trim())
         .Where(line => line.Length > 0)
         .ToList();
   }

   private static bool TryParseDate(string line, out DateOnly date)
   {
      return DateOnly.TryParseExact(
         $"{line} 2026",
         "d MMMM yyyy",
         CultureInfo.InvariantCulture,
         DateTimeStyles.None,
         out date
      );
   }

   private static TimeOnly? FindNextTime(
      IReadOnlyList<string> lines,
      int startIndex
   )
   {
      for(var index = startIndex; index < lines.Count; index++)
      {
         if(TimeOnly.TryParseExact(
            lines[index],
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var time
         ))
         {
            return time;
         }

         if(TryParseDate(lines[index], out _))
         {
            return null;
         }
      }

      return null;
   }

   private static IihfGame CreateGame(
      DateOnly date,
      TimeOnly time,
      string homeCode,
      string awayCode
   )
   {
      return new IihfGame(
         CreateGameId(date, homeCode, awayCode),
         CompetitionExternalId,
         CompetitionName,
         new DateTimeOffset(
            date.ToDateTime(time),
            TimeSpan.FromHours(2)
         ),
         "Scheduled",
         CreateTeam(homeCode),
         CreateTeam(awayCode)
      );
   }

   private static IihfTeam CreateTeam(string code)
   {
      var countryName = IihfCountryCodes.GetName(code);

      return new IihfTeam(
         $"{code.ToLowerInvariant()}-mens-ice-hockey",
         ToIsoCountryCode(code),
         countryName,
         $"{countryName} men's national ice hockey team"
      );
   }

   private static string CreateGameId(
      DateOnly date,
      string homeCode,
      string awayCode
   )
   {
      return $"iihf-2026-{date:MMdd}-{homeCode.ToLowerInvariant()}-" +
         awayCode.ToLowerInvariant();
   }

   private static string ToIsoCountryCode(string code)
   {
      return code switch
      {
         "SUI" => "CH",
         _ => code[..2]
      };
   }

   [GeneratedRegex(@"^(?<home>[A-Z]{3}) vs (?<away>[A-Z]{3})$")]
   private static partial Regex MatchupPattern();

   [GeneratedRegex(@"^(?<number>\d+)\s+[A-Z]+$")]
   private static partial Regex GameNumberPattern();

   [GeneratedRegex(StatsDateRegex)]
   private static partial Regex StatsDatePattern();

   [GeneratedRegex(@"^\d+\s+(?<stage>[A-Z]+)$")]
   private static partial Regex StagePattern();

   [GeneratedRegex(@"^(?<code>[A-Z]{3})$")]
   private static partial Regex TeamCodePattern();

   [GeneratedRegex(@"\s+")]
   private static partial Regex WhitespacePattern();
}
