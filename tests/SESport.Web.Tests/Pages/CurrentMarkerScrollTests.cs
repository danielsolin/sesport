namespace SESport.Core.Tests.Pages;

public sealed class CurrentMarkerScrollTests
{
   [Fact]
   public async Task ScriptScrollsCurrentMarkerBelowViewportTop()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var jsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/public-current-marker-scroll.js"
      );
      var js = await File.ReadAllTextAsync(jsPath);

      Assert.Contains(
         "document.querySelector(\".activity-now-marker\")",
         js
      );
      Assert.Contains("const topMargin = 12;", js);
      Assert.Contains("marker.getBoundingClientRect().top", js);
      Assert.Contains("window.scrollTo({", js);
      Assert.Contains("prefers-reduced-motion: reduce", js);
   }
}
