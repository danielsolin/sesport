namespace SESport.AI.ActivitySearch;

public interface IActivitySearchModelClient
{
   Task<ActivitySearchModelResult> SearchAsync(
      ActivitySearchRequest request,
      CancellationToken cancellationToken
   );
}
