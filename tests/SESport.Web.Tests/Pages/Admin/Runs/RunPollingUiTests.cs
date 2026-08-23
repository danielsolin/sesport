namespace SESport.Core.Tests.Pages.Admin.Runs;

public sealed class RunPollingUiTests
{
   [Fact]
   public async Task IndexPageWiresSummaryPollingUpdates()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Runs/Index.cshtml"
      );
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/Admin/js/admin-runs.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-run-statuses-url", html);
      Assert.Contains("data-ai-run-summary-cell", html);
      Assert.Contains("data-ai-run-status-cell", html);
      Assert.Contains("runSummaryCellSelector", script);
      Assert.Contains("resultSummary", script);
      Assert.Contains("updateRunRow(row, result);", script);
   }
}
