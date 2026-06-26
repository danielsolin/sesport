namespace SESport.Core.Tests.Pages.Admin.Broadcasts;

public sealed class ParticipationPollingUiTests
{
   [Fact]
   public async Task BroadcastsPageWiresStaleRetryGuard()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Broadcasts",
         "Index.cshtml"
      );
      var partialPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Broadcasts",
         "_BroadcastParticipationRuns.cshtml"
      );
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/site.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var partial = await File.ReadAllTextAsync(partialPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-check-participation-row", html);
      Assert.Contains("data-participation-runs-toggle", partial);
      Assert.Contains("broadcast-ai-check-runs-head", partial);
      Assert.DoesNotContain("broadcast-ai-check-retry", html);
      Assert.Contains("participationQueuedFromRunId", script);
      Assert.Contains("isStaleQueuedResult", script);
      Assert.Contains("getParticipationRunId", script);
      Assert.Contains("headCell.colSpan = 4", script);
      Assert.Contains("function getParticipationValue", script);
      Assert.Contains("check.participation", script);
   }
}
