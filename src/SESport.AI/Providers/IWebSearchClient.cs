namespace SESport.AI.Providers;

public interface IWebSearchClient
{
   Task<IReadOnlyList<WebSearchResult>> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken
   );
}
