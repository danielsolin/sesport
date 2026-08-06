namespace SESport.Core.Tests.Pages;

public sealed class CurrentMarkerScrollTests
{
   [Fact]
   public async Task ScriptScrollsFirstOngoingActivityBelowViewportTop()
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
         "document.querySelector(\n" +
         "      \".activity-agenda-section.activity-is-ongoing\"\n" +
         "   )",
         js
      );
      Assert.Contains("const topMargin = 12;", js);
      Assert.Contains(
         "ongoingActivity.getBoundingClientRect().top",
         js
      );
      Assert.Contains("window.scrollTo({", js);
      Assert.Contains("prefers-reduced-motion: reduce", js);
   }

   [Fact]
   public async Task ScriptSkipsScrollAfterPublicAutoReload()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var scrollScriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/public-current-marker-scroll.js"
      );
      var siteScriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/site.js"
      );
      var scrollScript = await File.ReadAllTextAsync(
         scrollScriptPath
      );
      var siteScript = await File.ReadAllTextAsync(siteScriptPath);

      Assert.Contains(
         "const autoReloadMarkerKey = " +
         "\"sesport-public-auto-reload\";",
         scrollScript
      );
      Assert.Contains(
         "window.sessionStorage.getItem(autoReloadMarkerKey)",
         scrollScript
      );
      Assert.Contains(
         "window.sessionStorage.removeItem(autoReloadMarkerKey)",
         scrollScript
      );
      Assert.Contains(
         "window.sessionStorage.setItem(",
         siteScript
      );
      Assert.Contains(
         "autoReloadMarkerKey",
         siteScript
      );
   }
}
