namespace SESport.Core.Tests.Pages.Admin;

public sealed class FormControlStyleTests
{
   [Fact]
   public async Task FormControlsUseSubgridColors()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var css = await File.ReadAllTextAsync(cssPath);

      Assert.Contains("border: 1px solid var(--line)", css);
      Assert.Contains("background: var(--subgrid-row)", css);
      Assert.Contains("border-color: var(--accent)", css);
      Assert.Contains("outline: 2px solid var(--subgrid-header)", css);
      Assert.Contains("background: var(--subgrid-row-hover)", css);
   }
}
