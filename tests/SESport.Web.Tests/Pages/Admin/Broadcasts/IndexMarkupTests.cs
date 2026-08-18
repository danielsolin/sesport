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
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains(
         "data-ajax-count-target=\"[data-broadcast-count]\"",
         html
      );
      Assert.Contains(
         "data-ajax-count-value=\"@Model.Broadcasts.Count\"",
         html
      );
      Assert.Contains("broadcastCountDecrementTarget", html);
      Assert.Contains("data-ajax-decrement-target=", html);
      Assert.Contains("filter-form-count", html);
      Assert.Contains("Broadcasts:", html);
      Assert.Contains("data-broadcast-count", html);
      Assert.Contains(
         "name=\"@RouteKeys.ShowHidden\"",
         html
      );
      Assert.Contains("value=\"false\"", html);
      Assert.Contains(
         "data-broadcast-inline-edit-field=\"title\"",
         html
      );
      Assert.Contains(
         "data-broadcast-inline-edit-field=\"description\"",
         html
      );
      Assert.Contains(
         "data-broadcast-inline-edit-field=\"channel\"",
         html
      );
      Assert.Contains(
         "\"start-time\"",
         html
      );
      Assert.Contains(
         "\"end-time\"",
         html
      );
      Assert.Contains("data-broadcast-description-text", html);
      Assert.Contains("Add description..", html);
      Assert.Contains("Add categories..", html);
      Assert.Contains("Edit broadcast description", html);
      Assert.Contains("Edit broadcast channel", html);
      Assert.Contains("Edit broadcast start time", html);
      Assert.Contains("Edit broadcast end time", html);
      Assert.Contains("<summary class=\"button\">Todo</summary>", html);
      Assert.Contains("asp-page-handler=\"AddTodo\"", html);
      Assert.Contains("<textarea name=\"text\"", html);
      Assert.Contains("Save", html);
   }
}
