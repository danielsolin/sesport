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
   }
}
