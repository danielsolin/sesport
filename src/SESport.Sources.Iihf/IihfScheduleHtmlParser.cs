using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace SESport.Sources.Iihf;

public sealed partial class IihfScheduleHtmlParser
{
   private const string CompetitionExternalId =
      "iihf-world-championship-2026";

   private const string CompetitionName =
      "2026 IIHF Ice Hockey World Championship";

   public IReadOnlyCollection<IihfGame> Parse(string html)
   {
      var lines = ExtractTextLines(html);
      var games = new List<IihfGame>();
      DateOnly? currentDate = null;

      for (var index = 0; index < lines.Count; index++)
      {
         var line = lines[index];

         if (TryParseDate(line, out var date))
         {
            currentDate = date;
            continue;
         }

         var match = MatchupPattern().Match(line);

         if (!match.Success || currentDate is null)
         {
            continue;
         }

         var homeCode = match.Groups["home"].Value;
         var awayCode = match.Groups["away"].Value;
         var time = FindNextTime(lines, index + 1);

         if (time is null)
         {
            continue;
         }

         games.Add(
            CreateGame(currentDate.Value, time.Value, homeCode, awayCode)
         );
      }

      return games;
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
      for (var index = startIndex; index < lines.Count; index++)
      {
         if (TimeOnly.TryParseExact(
            lines[index],
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var time
         ))
         {
            return time;
         }

         if (TryParseDate(lines[index], out _))
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

   [GeneratedRegex(@"\s+")]
   private static partial Regex WhitespacePattern();
}
