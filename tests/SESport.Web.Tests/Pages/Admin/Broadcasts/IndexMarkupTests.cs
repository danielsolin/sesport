namespace SESport.Core.Tests.Pages.Admin.Broadcasts;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task IndexPageShowsBroadcastCountInFilterRow()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Broadcasts/Index.cshtml"
      );
      var rowPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Broadcasts/_BroadcastRow.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var row = await File.ReadAllTextAsync(rowPath);
      var markup = html + row;
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var css = await File.ReadAllTextAsync(cssPath);

      Assert.Contains(
         "class=\"filter-form broadcast-filter-form\"",
         html
      );
      Assert.Contains(
         "name=\"@RouteKeys.TitleFilter\"",
         html
      );
      Assert.Contains("type=\"search\"", html);
      Assert.Contains(
         "oninput=\"submitFilterForm(this);\"",
         html
      );
      Assert.Contains(
         ".broadcast-filter-form .filter-sports",
         css
      );
      Assert.Contains("min-width: min(100%, 260px);", css);
      Assert.Contains(
         "data-ajax-count-target=\"[data-broadcast-count]\"",
         markup
      );
      Assert.Contains(
         "data-ajax-count-value=\"@Model.Broadcasts.Count\"",
         markup
      );
      Assert.Contains("data-ajax-decrement-target=", markup);
      Assert.Contains("filter-form-count", html);
      Assert.Contains("Broadcasts:", html);
      Assert.Contains("data-broadcast-count", html);
      Assert.Contains(
         "name=\"@RouteKeys.ShowHidden\"",
         markup
      );
      Assert.Contains("value=\"false\"", html);
      Assert.Contains(
         "data-broadcast-inline-edit-field=\"title\"",
         markup
      );
      Assert.Contains(
         "data-broadcast-inline-edit-field=\"description\"",
         markup
      );
      Assert.Contains(
         "data-broadcast-inline-edit-field=\"channel\"",
         markup
      );
      Assert.Contains("broadcast-channel-list", row);
      Assert.Contains("data-broadcast-inline-edit-display", row);
      Assert.Contains(
         "\"start-time\"",
         markup
      );
      Assert.Contains(
         "\"end-time\"",
         markup
      );
      Assert.Contains("data-broadcast-description-text", row);
      Assert.Contains("Add description..", row);
      Assert.Contains("Add categories..", row);
      Assert.Contains("Edit broadcast description", row);
      Assert.Contains("Edit broadcast channel", row);
      Assert.Contains("Edit broadcast start time", row);
      Assert.Contains("Edit broadcast end time", row);
      Assert.Contains("<summary class=\"button\">Todo</summary>", html);
      Assert.Contains("asp-page-handler=\"AddTodo\"", html);
      Assert.Contains("<textarea name=\"text\"", html);
      Assert.Contains("Save", html);
   }
}
