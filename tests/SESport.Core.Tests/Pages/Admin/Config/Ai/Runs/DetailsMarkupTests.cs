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

      Assert.Contains("<h2>System prompt</h2>", html);
      Assert.Contains("<h2>Rendered prompt</h2>", html);
      Assert.DoesNotContain("Tools description", html);
   }
}
