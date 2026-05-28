namespace SESport.Sources.Iihf;

public sealed class FileIihfScheduleClient(
   string filePath,
   IihfScheduleHtmlParser parser
) : IIihfScheduleClient
{
   public async Task<IReadOnlyCollection<IihfGame>> GetGamesAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   )
   {
      var html = await File.ReadAllTextAsync(filePath, cancellationToken);

      return parser
         .Parse(html)
         .Where(game => game.StartsAt >= request.StartsAfter)
         .Where(game => game.StartsAt <= request.StartsBefore)
         .ToList();
   }
}
