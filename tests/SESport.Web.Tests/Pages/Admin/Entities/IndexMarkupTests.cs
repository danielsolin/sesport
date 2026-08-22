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
         "src/SESport.Web/wwwroot/Admin/js/entities-index.js"
      );
      var partialPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Entities/_EntityRows.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);
      var partial = await File.ReadAllTextAsync(partialPath);

      Assert.Contains("data-entity-inline-edit-url", html);
      Assert.Contains("data-entity-search-url", html);
      Assert.Contains("data-person-facts-url", html);
      Assert.Contains("data-entity-list-container", html);
      Assert.Contains("data-entity-count", html);
      Assert.DoesNotContain("data-entity-watch-priority-template", html);
      Assert.Contains("AntiForgeryToken", html);
      Assert.Contains("<summary class=\"button\">Todo</summary>", html);
      Assert.Contains("asp-page-handler=\"AddTodo\"", html);
      Assert.Contains("<textarea name=\"text\"", html);
      Assert.Contains(
         "src=\"~/Admin/js/entities-index.js\"",
         html
      );
      Assert.Contains("format\", \"entity-rows\"", script);
      Assert.Contains("replaceContentsWithPartialHtml", script);
      Assert.DoesNotContain("createElement", script);
      Assert.DoesNotContain("innerHTML", script);
      Assert.DoesNotContain("renderEntityRowHtml", script);
      Assert.Contains("data-person-facts-form", script);
      Assert.Contains("data-person-facts-form", partial);
      Assert.Contains("broadcast-participation-check-link", partial);
      Assert.Contains("Facts", partial);
      Assert.Contains("data-entity-inline-edit-field", partial);
      Assert.Contains("data-entity-inline-edit-display", partial);
      Assert.Contains("data-entity-inline-edit-input", partial);
      Assert.Contains("Add watch priority..", partial);
      Assert.Contains("data-entity-list-partial-body", partial);
      Assert.Contains(
         "/Pages/Admin/Entities/_EntityRows.cshtml",
         html
      );
      Assert.Contains("data-entity-inline-edit-field", partial);
      Assert.Contains("data-person-facts-form", partial);
      Assert.Contains("AntiForgeryToken", partial);
   }
}
