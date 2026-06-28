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
      var js = await File.ReadAllTextAsync(jsPath);

      Assert.Contains("__RequestVerificationToken", js);
      Assert.Contains("getAntiForgeryToken", js);
   }
}
