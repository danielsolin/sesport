namespace SESport.Core.Tests.Pages;

public sealed class PastActivitiesToggleTests
{
   [Fact]
   public async Task IndexPageIncludesPastActivityToggle()
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
      Assert.Contains("data-activity-agenda", page);
      Assert.Contains("data-activity-past-toggle", page);
      Assert.Contains("Visa Tidigare", page);
      Assert.Contains("Dölj Tidigare", page);
      Assert.Contains(
         "<script src=\"~/js/public-past-activities.js\"",
         page
      );
      Assert.Contains(
         ".activity-past-activities-hidden\n" +
         "   .activity-past-activity-hidden {\n" +
         "   display: none;",
         css
      );
      Assert.Contains(
         ".activity-past-toggle-row {",
         css
      );
      Assert.Contains(
         "const hiddenClass = \"activity-past-activities-hidden\";",
         script
      );
      Assert.Contains(
         "agenda.classList.toggle(hiddenClass);",
         script
      );
      Assert.Contains(
         "toggle.setAttribute(\"aria-expanded\", String(!isHidden));",
         script
      );
   }
}
