namespace SESport.Sources.Iihf;

public sealed class IihfScheduleDocumentClient(
   HttpClient httpClient,
   IihfScheduleHtmlParser parser,
   Uri documentUri
) : IIihfScheduleClient
{
   public async Task<IReadOnlyCollection<IihfGame>> GetGamesAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   )
   {
      var html = await httpClient.GetStringAsync(
         documentUri,
         cancellationToken
      );

      return parser
         .Parse(html)
         .Where(game => game.StartsAt >= request.StartsAfter)
         .Where(game => game.StartsAt <= request.StartsBefore)
         .ToList();
   }
}
