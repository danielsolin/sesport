namespace SESport.AI.WebPages;

public interface IWebPageContentClient
{
   Task<WebPageContent?> FetchAsync(
      string url,
      CancellationToken cancellationToken
   );
}
