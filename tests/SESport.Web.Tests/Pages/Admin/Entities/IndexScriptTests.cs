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
         "src/SESport.Web/wwwroot/Admin/js/entities-index.js"
      );
      var partialPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Entities/_EntityRows.cshtml"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var js = await File.ReadAllTextAsync(jsPath);
      var partial = await File.ReadAllTextAsync(partialPath);
      var css = await File.ReadAllTextAsync(cssPath);

      Assert.Contains("AntiForgeryToken", partial);
      Assert.Contains("data-person-facts-status", js);
      Assert.Contains("Facts job failed.", js);
      Assert.Contains("ses-entity-search-link-missing", partial);
      Assert.Contains("Search for missing age", partial);
      Assert.Contains("replaceContentsWithPartialHtml", js);
      Assert.Contains("DOMContentLoaded", js);
      Assert.Contains("initializeEntitySearch();", js);
      Assert.DoesNotContain("createElement", js);
      Assert.DoesNotContain("innerHTML", js);
      Assert.Contains(".ses-entity-search-link-missing {", css);
      Assert.Contains("background: #fff7e5", css);
   }
}
