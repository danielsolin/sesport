namespace SESport.Core.Tests.Pages.Admin.Entities;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task IndexPageExposesEntityInlineEditingHooks()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Entities/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("data-entity-inline-edit-url", html);
      Assert.Contains("data-entity-search-url", html);
      Assert.Contains("data-entity-list-body", html);
      Assert.Contains("data-entity-row-id", html);
      Assert.Contains("data-entity-inline-edit-field", html);
      Assert.Contains("data-entity-inline-edit-display", html);
      Assert.Contains("data-entity-inline-edit-input", html);
      Assert.Contains("AntiForgeryToken", html);
      Assert.Contains("entities-index.js", html);
   }
}
