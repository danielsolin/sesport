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
   [WebGetPageDescription]
   public async Task<WebPageToolResponse?> FetchPageAsync(
      [Description("The absolute URL of the page to fetch.")]
      string url,
      CancellationToken cancellationToken = default
   )
   {
      var content = await PageContentClient.FetchAsync(
         url,
         cancellationToken
      );

      return content is null
         ? null
         : WebPageToolResponse.From(content);
   }
}
