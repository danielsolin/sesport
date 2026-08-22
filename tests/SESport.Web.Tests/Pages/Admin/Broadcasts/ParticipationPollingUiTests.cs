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
         "src/SESport.Web/wwwroot/Admin/js/site.js"
      );
      var rowPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Broadcasts/_BroadcastRow.cshtml"
      );
      var runsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Broadcasts/_BroadcastParticipationRuns.cshtml"
      );
      var resultsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Ajax/Poll/_ParticipationStatusResults.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);
      var row = await File.ReadAllTextAsync(rowPath);
      var runs = await File.ReadAllTextAsync(runsPath);
      var results = await File.ReadAllTextAsync(resultsPath);

      Assert.Contains("data-broadcast-results", html);
      Assert.Contains("data-check-participation-row", row);
      Assert.Contains("data-ajax-success=\"toggle-visibility\"", row);
      Assert.Contains("data-participation-cell", row);
      Assert.Contains("data-broadcast-group-participants", row);
      Assert.Contains(
         "data-broadcast-group-participants-clear",
         row
      );
      Assert.Contains("data-broadcast-activity-link", row);
      Assert.Contains("Create Activity", runs);
      Assert.Contains("data-broadcast-categories-list", row);
      Assert.Contains("_BroadcastParticipationRuns", results);
      Assert.Contains(
         "/Pages/Admin/Broadcasts/_BroadcastParticipationRuns.cshtml",
         results
      );
      Assert.DoesNotContain(
         "\"/Admin/Broadcasts/_BroadcastParticipationRuns",
         results
      );
      Assert.DoesNotContain("broadcast-ai-check-retry", runs);
      Assert.Contains("initializeParticipationRunsAsync", script);
      Assert.Contains("initializeBroadcastParticipantClearing", script);
      Assert.Contains("clearParticipantsQueryKey", script);
      Assert.Contains("participantList.remove()", script);
      Assert.Contains("participationQueuedFromRunId", script);
      Assert.Contains("getParticipationRunId", script);
      Assert.Contains("const queuingParticipationIds = new Set()", script);
      Assert.Contains("queuingParticipationIds.has(broadcastId)", script);
      Assert.Contains("queuingParticipationIds.delete(broadcastId)", script);
      var checkButtonIndex = html.IndexOf(
         "data-broadcast-results",
         StringComparison.Ordinal
      );
      var checkButtonContext = html[
         Math.Max(0, checkButtonIndex - 400)..checkButtonIndex
      ];
      Assert.DoesNotContain("@if", checkButtonContext);
      Assert.Contains("updateBroadcastVisibilityAsync", script);
      Assert.Contains("getPartialRootFromHtml", script);
      Assert.Contains("replaceContentsWithPartialHtml", script);
      Assert.DoesNotContain("createElement", script);
      Assert.DoesNotContain("innerHTML", script);
      Assert.Contains("broadcast-ai-check-participant-template-input", runs);
      Assert.Contains("organizationSportName", runs);
      Assert.Contains("syncReplacementCount", script);
      Assert.Contains("ajaxCountTarget", script);
      Assert.Contains("ajaxCountValue", script);
      Assert.Contains(
         "data-participation-partial",
         results
      );
   }
}
