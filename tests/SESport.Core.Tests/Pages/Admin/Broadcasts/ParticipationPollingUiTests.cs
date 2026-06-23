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
         "src/SESport.Web/Pages/Admin/Broadcasts/_BroadcastParticipationRuns.cshtml"
      );
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/site.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-check-participation-row", html);
      Assert.Contains("data-participation-runs-toggle", html);
      Assert.Contains("broadcast-ai-check-runs-head", html);
      Assert.DoesNotContain("broadcast-ai-check-retry", html);
      Assert.Contains("participationQueuedFromRunId", script);
      Assert.Contains("isStaleQueuedResult", script);
      Assert.Contains("getParticipationRunId", script);
   }
}
