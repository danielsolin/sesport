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
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/entities-index.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-entity-inline-edit-url", html);
      Assert.Contains("data-entity-search-url", html);
      Assert.Contains("data-person-facts-url", html);
      Assert.Contains("data-entity-list-body", html);
      Assert.Contains("data-entity-count", html);
      Assert.Contains("data-entity-watch-priority-template", html);
      Assert.Contains("AntiForgeryToken", html);
      Assert.Contains("<summary class=\"button\">Todo</summary>", html);
      Assert.Contains("asp-page-handler=\"AddTodo\"", html);
      Assert.Contains("<textarea name=\"text\"", html);
      Assert.Contains("entities-index.js", html);
      Assert.Contains("renderEntityRowHtml", script);
      Assert.Contains("count.textContent = value", script);
      Assert.Contains("data-person-facts-form", script);
      Assert.Contains("broadcast-participation-check-link", script);
      Assert.Contains("Facts", script);
      Assert.Contains("data-entity-inline-edit-field", script);
      Assert.Contains("data-entity-inline-edit-display", script);
      Assert.Contains("data-entity-inline-edit-input", script);
      Assert.Contains("renderWatchPriorityOptions", script);
   }
}
