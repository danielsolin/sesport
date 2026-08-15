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
      Assert.Contains("data-activity-past-toggle", page);
      Assert.Contains("public-past-activities.js", page);
      Assert.Contains("event.preventDefault();", script);
      Assert.Contains("const scrollX = window.scrollX;", script);
      Assert.Contains("const scrollY = window.scrollY;", script);
      Assert.Contains("window.scrollTo({", script);
      Assert.Contains("behavior: \"auto\"", script);
      Assert.Contains(
         "window.requestAnimationFrame(() => {\n" +
         "         window.requestAnimationFrame(restoreScrollPosition);",
         script
      );
   }
}
