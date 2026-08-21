namespace SESport.Core.Tests.Pages;

public sealed class PastActivitiesVisibilityTests
{
   [Fact]
   public async Task IndexPageHidesAllPastActivitiesOnToday()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var pagePath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Index.cshtml"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/public.css"
      );
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/public-past-activities.js"
      );
      var page = await File.ReadAllTextAsync(pagePath);
      var css = await File.ReadAllTextAsync(cssPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains(
         "var shouldHidePastActivities = Model.IsSportToday &&",
         page
      );
      Assert.Contains(
         "var pastActivityVisibilityClass = shouldHidePastActivities &&\n" +
         "            agendaSection.HasEnded",
         page
      );
      Assert.Contains(
         ".activity-past-activities-hidden\n" +
         "   .activity-past-activity-hidden {\n" +
         "   display: none;",
         css
      );
      Assert.DoesNotContain("isLatestPastActivity", page);
      Assert.DoesNotContain("pastActivityToggleIndex", page);
      Assert.Contains("data-activity-past-toggle", page);
      Assert.Contains(
         "var activityAnchorId = \"activity-\" + " +
         "activity.Id.ToString(\"N\");",
         page
      );
      Assert.Contains(
         "<article id=\"@activityAnchorId\"",
         page
      );
      Assert.Contains(
         "Model.HasPublishedActivitiesTomorrow",
         page
      );
      Assert.Contains("activity-tomorrow-link-row", page);
      Assert.Contains("activity-tomorrow-link", page);
      Assert.Contains("Model.TomorrowDate", page);
      var markerIndex = page.IndexOf(
         "if(timelineEntry.IsCurrentMarker)",
         StringComparison.Ordinal
      );
      var toggleIndex = page.IndexOf(
         "data-activity-past-toggle",
         StringComparison.Ordinal
      );
      var nowMarkerIndex = page.IndexOf(
         "<div class=\"activity-now-marker\">",
         StringComparison.Ordinal
      );
      Assert.True(markerIndex >= 0);
      Assert.True(toggleIndex > markerIndex);
      Assert.True(toggleIndex < nowMarkerIndex);
      Assert.Contains("public-past-activities.js", page);
      Assert.Contains("event.preventDefault();", script);
      Assert.DoesNotContain("window.scrollTo({", script);
      Assert.Contains("toggle.scrollIntoView({", script);
      Assert.Contains("behavior: \"smooth\"", script);
      Assert.Contains("block: \"center\"", script);
      Assert.Contains(
         "sesport-public-past-activities-expanded",
         script
      );
      Assert.Contains("window.sessionStorage.getItem(", script);
      Assert.Contains("window.sessionStorage.setItem(", script);
      Assert.Contains("window.sessionStorage.removeItem(", script);
      Assert.Contains("window.location.href", script);
      Assert.Contains("window.location.hash.slice(1)", script);
      Assert.Contains("target.scrollIntoView({", script);
      Assert.Contains(
         "target.closest(\n" +
         "         \".activity-past-activity-hidden\"",
         script
      );
      Assert.Contains(
         "window.requestAnimationFrame(() => {\n" +
         "         window.requestAnimationFrame(scrollToToggle);",
         script
      );
   }
}
