using SESport.AI.ActivitySearch;

namespace SESport.AI.Interfaces;

public interface IActivitySearchModelClient
{
   Task<ActivitySearchModelResult> SearchAsync(
      ActivitySearchRequest request,
      CancellationToken cancellationToken
   );
}
