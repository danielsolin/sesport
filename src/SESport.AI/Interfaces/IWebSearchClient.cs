using SESport.AI.Providers;

namespace SESport.AI.Interfaces;

public interface IWebSearchClient
{
   Task<IReadOnlyList<WebSearchResult>> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken
   );
}
