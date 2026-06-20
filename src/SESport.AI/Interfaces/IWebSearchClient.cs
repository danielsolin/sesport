using SESport.AI.Providers;

namespace SESport.AI.Interfaces;

public interface IWebSearchClient
{
   Task<WebSearchResponse> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken
   );
}
