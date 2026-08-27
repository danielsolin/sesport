using System.Text.Json;

using SESport.AI.WebPages;
using SESport.MCP;

namespace SESport.Core.Tests.MCP;

public sealed class WebPageToolTests
{
   [Fact]
   public async Task FetchPageDoesNotExposeInternalFullText()
   {
      const string internalFullText =
         "internal-main-text-full-that-must-not-cross-the-mcp-boundary";
      var client = new StubWebPageContentClient(
         new WebPageContent(
            "Test page",
            "https://example.test/page",
            null,
            ["Heading"],
            "Capped main text",
            true,
            internalFullText,
            Fetcher: "test"
         )
      );
      var tool = new WebPageTool(client);

      var response = await tool.FetchPageAsync(
         "https://example.test/page"
      );

      Assert.NotNull(response);
      Assert.Equal("Capped main text", response!.MainText);
      var serialized = JsonSerializer.Serialize(response);
      Assert.DoesNotContain("MainTextFull", serialized);
      Assert.DoesNotContain("mainTextFull", serialized);
      Assert.DoesNotContain(internalFullText, serialized);
   }

   private sealed class StubWebPageContentClient(
      WebPageContent content
   ) : IWebPageContentClient
   {
      public Task<WebPageContent?> FetchAsync(
         string url,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<WebPageContent?>(content);
      }
   }
}
