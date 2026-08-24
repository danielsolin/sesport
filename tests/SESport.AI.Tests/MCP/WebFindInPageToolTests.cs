using SESport.AI.WebPages;
using SESport.MCP;

namespace SESport.Core.Tests.MCP;

public sealed class WebFindInPageToolTests
{
   [Fact]
   public async Task FindInPageUsesFullTextAndReturnsCompactMatches()
   {
      var client = new StubWebPageContentClient(
         new WebPageContent(
            "Test page",
            "https://example.test/page",
            null,
            [],
            "Capped text",
            true,
            "The internal phrase appears in the full page text."
         )
      );
      var tool = new WebFindInPageTool(client);

      var result = await tool.FindInPageAsync(
         "https://example.test/page",
         "INTERNAL PHRASE"
      );

      Assert.Contains("internal phrase", result);
   }

   [Fact]
   public async Task FindInPageRejectsMissingSearchTerm()
   {
      var tool = new WebFindInPageTool(
         new StubWebPageContentClient(null)
      );

      var result = await tool.FindInPageAsync(
         "https://example.test/page",
         " "
      );

      Assert.Equal("Missing search term.", result);
   }

   private sealed class StubWebPageContentClient(
      WebPageContent? content
   ) : IWebPageContentClient
   {
      public Task<WebPageContent?> FetchAsync(
         string url,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult(content);
      }
   }
}
