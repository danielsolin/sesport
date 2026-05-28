namespace SESport.Sources.Iihf;

public sealed class InMemoryIihfScheduleClient(
   IReadOnlyCollection<IihfGame> games
) : IIihfScheduleClient
{
   public Task<IReadOnlyCollection<IihfGame>> GetGamesAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   )
   {
      IReadOnlyCollection<IihfGame> matchingGames = games
         .Where(game => game.StartsAt >= request.StartsAfter)
         .Where(game => game.StartsAt <= request.StartsBefore)
         .ToList();

      return Task.FromResult(matchingGames);
   }
}
