using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace SESport.MCP;

public sealed class WebPageTool
{
   private readonly IWebPageContentClient PageContentClient;

   public WebPageTool(IWebPageContentClient pageContentClient)
   {
      PageContentClient = pageContentClient;
   }

   [McpServerTool(
      Name = "web_get_page",
      UseStructuredContent = true
   )]
   [Description(
      "Fetches a web page through the project's existing web page " +
         "content pipeline and returns the raw WebPageContent " +
         "unchanged."
   )]
   public Task<WebPageContent?> FetchPageAsync(
      [Description("The absolute URL of the page to fetch.")]
      string url,
      CancellationToken cancellationToken = default
   )
   {
      return PageContentClient.FetchAsync(url, cancellationToken);
   }
}
