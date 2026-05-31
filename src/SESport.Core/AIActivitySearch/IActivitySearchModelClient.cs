namespace SESport.Core.AIActivitySearch;

public interface IActivitySearchModelClient
{
   Task<ActivitySearchModelResult> SearchAsync(
      ActivitySearchRequest request,
      CancellationToken cancellationToken
   );
}
