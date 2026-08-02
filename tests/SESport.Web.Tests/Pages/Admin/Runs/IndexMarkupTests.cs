namespace SESport.Core.Tests.Pages.Admin.Runs;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task IndexPageExposesRunPollingHooks()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Runs/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("data-run-statuses-url", html);
      Assert.Contains("data-ai-run-id", html);
      Assert.Contains("data-ai-run-status-cell", html);
      Assert.Contains("data-ai-run-status-text", html);
      Assert.Contains("data-ai-run-rounds-cell", html);
      Assert.Contains("data-ai-run-duration-cell", html);
      Assert.Contains(">MAXCH<", html);
      Assert.Contains(">NAME<", html);
      Assert.Contains("ai-runs-name-link", html);
      Assert.Contains("GetDetailsRouteValues(run.Id)", html);
      Assert.Contains("GetDeleteRouteValues(run.Id)", html);
      Assert.Contains("AntiForgeryToken", html);
      Assert.Contains("multiple=\"multiple\"", html);
      Assert.DoesNotContain(">Apply<", html);
      Assert.DoesNotContain(">Reset<", html);
      Assert.DoesNotContain(">View<", html);
   }
}
