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
      Assert.Contains("activity-ongoing-dots", html);
      Assert.Contains(
         "@keyframes activity-ongoing-dots",
         css
      );
      Assert.Contains(
         "@media (prefers-reduced-motion: reduce)",
         css
      );
      Assert.Contains("@media (orientation: portrait)", css);
      Assert.Contains(
         ".activity-now-marker {\n      display: none;",
         css
      );
      Assert.DoesNotContain(
         ".activity-now-marker-line:first-child",
         css
      );
      Assert.DoesNotContain(
         ".activity-now-marker-line:last-child",
         css
      );
      Assert.Contains(
         ".activity-now-marker-badge {\n      padding: 5px 10px;",
         css
      );
      Assert.Contains(
         ".activity-is-ongoing .activity-entry,\n" +
         "   .activity-has-ended .activity-entry {\n" +
         "      padding-top: 52px;",
         css
      );
      Assert.Contains(
         ".activity-status-badge {\n      right: auto;\n" +
         "      left: 14px;",
         css
      );
      Assert.Contains(
         ".activity-entry-sport-icon {\n      right: 14px;\n" +
         "      left: auto;",
         css
      );
      Assert.Contains(
         "max-width: none;\n      padding-right: 0;\n" +
         "      overflow: visible;",
         css
      );
      Assert.Contains(
         "@media (max-width: 600px) and (orientation: portrait)",
         css
      );
      Assert.DoesNotContain(
         ".index-participants-filter.is-selected::before",
         css
      );
      Assert.Contains(
         ".index-participants-filter.is-selected::after",
         css
      );
      Assert.Contains("bottom: -9px;", css);
      Assert.DoesNotContain(
         ".activity-has-ended {\n   opacity: 0.5;",
         css
      );
      Assert.Contains(
         ".activity-has-ended .activity-ended-badge",
         css
      );
      Assert.Contains(
         ".activity-entry > :not(.activity-ended-badge)",
         css
      );
      Assert.Contains(
         ".activity-has-ended .activity-entry",
         css
      );
      Assert.Contains(
         "border-color: rgba(0, 106, 168, 0.5);",
         css
      );
      Assert.Contains(
         "--activity-duration-color: #7d8589;",
         css
      );
      Assert.Contains(
         "box-shadow: 0 0 2px rgba(125, 133, 137, 0.1);",
         css
      );
   }
}
