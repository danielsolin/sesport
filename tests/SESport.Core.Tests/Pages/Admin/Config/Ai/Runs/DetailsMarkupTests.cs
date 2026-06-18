namespace SESport.Core.Tests.Pages.Admin.Config.Ai.Runs;

public sealed class DetailsMarkupTests
{
   [Fact]
   public async Task DetailsPageDoesNotShowToolsDescription()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Config/Ai/Runs/Details.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("summary>Input payload</summary>", html);
      Assert.Contains("summary>System prompt</summary>", html);
      Assert.Contains("summary>Rendered prompt</summary>", html);
      Assert.Contains("summary>Output</summary>", html);
      Assert.Contains("<dt>Temperature</dt>", html);
      Assert.Contains("Final conversation chars", html);
      Assert.Contains("Max conversation chars", html);
      Assert.Contains("tool-trace-turn-header-main", html);
      Assert.DoesNotContain("Full trace", html);
      Assert.Contains("Round", html);
      Assert.DoesNotContain("Tools description", html);
   }
}
