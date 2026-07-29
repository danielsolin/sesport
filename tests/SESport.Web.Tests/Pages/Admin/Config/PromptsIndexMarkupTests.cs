namespace SESport.Core.Tests.Pages.Admin.Config;

public sealed class PromptsIndexMarkupTests
{
   [Fact]
   public async Task IndexPageSeparatesCurrentAndUnusedPrompts()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Config/Ai/Prompts/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("Title = \"Active\"", html);
      Assert.Contains("Title = \"Inactive\"", html);
      Assert.Contains("Model.CurrentPrompts", html);
      Assert.Contains("Model.UnusedPrompts", html);
      Assert.Contains("<th>MaxOT</th>", html);
      Assert.Contains("<th>MaxTR</th>", html);
      Assert.Contains("<th>MinTR</th>", html);
      Assert.DoesNotContain("<th>PROMPT</th>", html);
   }
}
