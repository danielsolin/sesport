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
      Assert.Contains("activity-participant-col-name", html);
      Assert.Contains("activity-participant-col-age", html);
      Assert.Contains("activity-participant-col-height", html);
      Assert.Contains("activity-participant-col-country", html);
      Assert.Contains(
         "@media (max-width: 600px) and (orientation: portrait)",
         css
      );
   }
}
