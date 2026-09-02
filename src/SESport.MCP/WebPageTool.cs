using Microsoft.Extensions.Logging.Abstractions;

namespace SESport.MCP;

public sealed class WebPageTool
{
   private readonly IWebPageContentClient PageContentClient;
   private readonly ILogger<WebPageTool> Logger;

   public WebPageTool(
      IWebPageContentClient pageContentClient,
      ILogger<WebPageTool>? logger = null
   )
   {
      PageContentClient = pageContentClient;
      Logger = logger ?? NullLogger<WebPageTool>.Instance;
   }

   [McpServerTool(
      Name = "web_get_page",
      UseStructuredContent = true
   )]
   [WebGetPageDescription]
   public async Task<WebPageToolResponse?> FetchPageAsync(
      [Description("The absolute URL of the page to fetch.")]
      string url,
      CancellationToken cancellationToken = default
   )
   {
      try
      {
         var content = await PageContentClient.FetchAsync(
            url,
            cancellationToken
         );

         return content is null
            ? null
            : WebPageToolResponse.From(content);
      }
      catch(OperationCanceledException)
      {
         Logger.LogWarning(
            "web_get_page canceled while fetching {Url}; returning no " +
            "result.",
            url
         );
         return null;
      }
   }
}
