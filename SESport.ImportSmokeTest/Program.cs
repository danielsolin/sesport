using SESport.Core.Ingestion;
using SESport.Sources.Iihf;

var client = new IihfScheduleDocumentClient(
   new HttpClient(),
   new IihfScheduleHtmlParser(),
   new Uri("https://stats.iihf.com/Hydra/969/index.html")
);

var games = await client.GetGamesAsync(
   new ImportRequest(
      new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
      new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
   ),
   CancellationToken.None
);

foreach(var game in games)
{
   Console.WriteLine($"{game.StartsAt:u} {game.HomeTeam.CountryName} vs {game.AwayTeam.CountryName} ({game.Stage})");
}
