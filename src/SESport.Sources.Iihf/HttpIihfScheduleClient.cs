namespace SESport.Sources.Iihf;

public sealed class HttpIihfScheduleClient(
   HttpClient httpClient,
   IihfScheduleHtmlParser parser,
   Uri scheduleUri
) : IIihfScheduleClient
{
   public async Task<IReadOnlyCollection<IihfGame>> GetGamesAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   )
   {
      var html = await httpClient.GetStringAsync(
         scheduleUri,
         cancellationToken
      );

      return parser
         .Parse(html)
         .Where(game => game.StartsAt >= request.StartsAfter)
         .Where(game => game.StartsAt <= request.StartsBefore)
         .ToList();
   }
}
