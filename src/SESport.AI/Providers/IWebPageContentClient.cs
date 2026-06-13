namespace SESport.AI.Providers;

public interface IWebPageContentClient
{
   Task<WebPageContent?> FetchAsync(
      string url,
      CancellationToken cancellationToken
   );
}
