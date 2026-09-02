namespace SESport.AI.WebSearch;

public interface IWebSearchClient
{
   Task<WebSearchResponse> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken,
      int searchAttempt = 0,
      bool includeSocialMedia = false
   );
}
