namespace SESport.Core.Tests.Pages;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task IndexPageIncludesParticipantsCount()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Index.cshtml"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/public.css"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var css = await File.ReadAllTextAsync(cssPath);

      Assert.Contains("index-participants-info", html);
      Assert.Contains("aria-label=\"Visa alla sporter\"", html);
      Assert.Contains("TotalParticipantsCount", html);
      Assert.Contains("Svenskar:", html);
      Assert.Contains("SportParticipantCounts", html);
      Assert.Contains(
         "aria-label=\"Visa endast @sport.SportName\"",
         html
      );
      Assert.Contains("index-participants-filter", html);
      Assert.Contains("is-selected", html);
      Assert.Contains("asp-route-sport=\"@sport.SportId\"", html);
      Assert.Contains("data-date-dropdown", html);
      Assert.Contains("data-date-dropdown-toggle", html);
      Assert.Contains("data-date-dropdown-menu", html);
      Assert.Contains("public-date-select.js", html);
      Assert.DoesNotContain("date-select-input", html);
      Assert.Contains("activity-participant-col-name", html);
      Assert.Contains("activity-participant-col-age", html);
      Assert.DoesNotContain("activity-participant-col-height", html);
      Assert.Contains("activity-participant-col-country", html);
      Assert.Contains("activity-now-marker", html);
      Assert.Contains("@media (orientation: portrait)", css);
      Assert.Contains(
         ".activity-now-marker {\n      display: none;",
         css
      );
      Assert.Contains(
         ".activity-now-marker-line:first-child",
         css
      );
      Assert.Contains(
         ".activity-now-marker-line:last-child",
         css
      );
      Assert.Contains(
         "@media (max-width: 600px) and (orientation: portrait)",
         css
      );
      Assert.Contains(
         ".index-participants-filter.is-selected::before",
         css
      );
   }
}
