namespace SESport.Sources.Iihf;

public interface IIihfScheduleClient
{
   Task<IReadOnlyCollection<IihfGame>> GetGamesAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   );
}
