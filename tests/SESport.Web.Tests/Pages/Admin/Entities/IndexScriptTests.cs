namespace SESport.Core.Tests.Pages.Admin.Entities;

public sealed class IndexScriptTests
{
   [Fact]
   public async Task IndexScriptIncludesAntiForgeryTokenInDeleteForms()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var jsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/entities-index.js"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var js = await File.ReadAllTextAsync(jsPath);
      var css = await File.ReadAllTextAsync(cssPath);

      Assert.Contains("__RequestVerificationToken", js);
      Assert.Contains("getAntiForgeryToken", js);
      Assert.Contains("data-person-facts-status", js);
      Assert.Contains("Facts job failed.", js);
      Assert.Contains("ses-entity-search-link-missing", js);
      Assert.Contains("Search for missing age", js);
      Assert.Contains(".ses-entity-search-link-missing {", css);
      Assert.Contains("background: #fff7e5", css);
   }
}
