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
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/site.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-check-participation-row", html);
      Assert.Contains("data-ajax-success=\"toggle-visibility\"", html);
      Assert.Contains("data-participation-cell", html);
      Assert.Contains("broadcast.ActivityGroupParticipants", html);
      Assert.Contains("Create Activity", html);
      Assert.Contains("data-broadcast-categories-list", html);
      Assert.DoesNotContain("_BroadcastParticipationRuns", html);
      Assert.DoesNotContain("broadcast-ai-check-retry", html);
      Assert.Contains("initializeParticipationRunsAsync", script);
      Assert.Contains("setNoParticipationHistoryCell", script);
      Assert.Contains("renderBroadcastCategories", script);
      Assert.Contains("participationQueuedFromRunId", script);
      Assert.Contains("isStaleQueuedResult", script);
      Assert.Contains("getParticipationRunId", script);
      Assert.Contains("const queuingParticipationIds = new Set()", script);
      Assert.Contains("queuingParticipationIds.has(broadcastId)", script);
      Assert.Contains("queuingParticipationIds.delete(broadcastId)", script);
      Assert.DoesNotContain("syncParticipationCheckButton", script);
      var checkButtonIndex = html.IndexOf(
         "data-check-participation-row",
         StringComparison.Ordinal
      );
      var checkButtonContext = html[
         Math.Max(0, checkButtonIndex - 400)..checkButtonIndex
      ];
      Assert.DoesNotContain("@if", checkButtonContext);
      Assert.Contains("updateBroadcastVisibilityAsync", script);
      Assert.Contains("headCell.colSpan = 4", script);
      Assert.Contains("function getParticipationValue", script);
      Assert.Contains("createParticipantSearchLink", script);
      Assert.Contains("getParticipationSportName", script);
      Assert.Contains("organizationSportName", script);
      Assert.Contains("encodeURIComponent(searchQuery)", script);
      Assert.Contains("check.participation", script);
      Assert.Contains("syncReplacementCount", script);
      Assert.Contains("ajaxCountTarget", script);
      Assert.Contains("ajaxCountValue", script);
      Assert.Contains(
         "const broadcastCountSelector = \"[data-broadcast-count]\";",
         script
      );
      Assert.Contains(
         "visibilityForm.dataset.ajaxDecrementTarget =",
         script
      );
   }
}
