namespace SESport.MCP;

public sealed class WebSearchTool
{
   private readonly IWebSearchClient SearchClient;

   public WebSearchTool(IWebSearchClient searchClient)
   {
      SearchClient = searchClient;
   }

   [McpServerTool(
      Name = "web_search",
      UseStructuredContent = true
   )]
   [Description(
      "Searches the web through SESport's local SearXNG instance " +
      "and returns the raw WebSearchResponse (results, provider, " +
      "details). Standard search operators such as site:, filetype:, " +
      "and quoted exact phrases are passed to SearXNG."
   )]
   public Task<WebSearchResponse> WebSearchAsync(
      [Description("The search query.")]
      string query,
      [Description("Maximum number of results to return. Defaults to 5.")]
      int maxResults = 5,
      CancellationToken cancellationToken = default
   )
   {
      return SearchClient.SearchAsync(
         query,
         maxResults,
         cancellationToken,
         searchAttempt: 0,
         includeSocialMedia: false
      );
   }
}
