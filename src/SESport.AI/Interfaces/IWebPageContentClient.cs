using SESport.AI.Providers;

namespace SESport.AI.Interfaces;

public interface IWebPageContentClient
{
   Task<WebPageContent?> FetchAsync(
      string url,
      CancellationToken cancellationToken
   );
}
