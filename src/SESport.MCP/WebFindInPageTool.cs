namespace SESport.MCP;

public sealed class WebFindInPageTool(
   IWebPageContentClient pageContentClient
)
{
   [McpServerTool(Name = "web_find_in_page")]
   [WebFindInPageDescription]
   public async Task<string> FindInPageAsync(
      [Description("The absolute URL of the page to search.")]
      string url,
      [Description("The text to find, matched case-insensitively.")]
      string find,
      CancellationToken cancellationToken = default
   )
   {
      if(string.IsNullOrWhiteSpace(find))
      {
         return "Missing search term.";
      }

      var pageContent = await pageContentClient.FetchAsync(
         url,
         cancellationToken
      );

      if(pageContent is null)
      {
         return $"Unable to fetch page content from {url}.";
      }

      if(!string.IsNullOrWhiteSpace(pageContent.FetchErrorMessage))
      {
         return pageContent.FetchErrorMessage.Trim();
      }

      return WebPageToolSupport.FindInPage(pageContent, find);
   }
}
