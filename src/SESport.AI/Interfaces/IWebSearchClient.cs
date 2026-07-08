using SESport.AI.WebSearch;

namespace SESport.AI.Interfaces;

public interface IWebSearchClient
{
   Task<WebSearchResponse> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken,
      int searchAttempt = 0
   );
}
