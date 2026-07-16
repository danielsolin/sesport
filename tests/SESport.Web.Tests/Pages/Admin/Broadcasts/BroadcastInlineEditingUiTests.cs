namespace SESport.Core.Tests.Pages.Admin.Broadcasts;

public sealed class BroadcastInlineEditingUiTests
{
   [Fact]
   public async Task BroadcastsPageExportsInlineEditingInitializer()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var siteJsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/site.js"
      );
      var broadcastInlineEditJsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/broadcast-inline-edit.js"
      );
      var siteJs = await File.ReadAllTextAsync(siteJsPath);
      var broadcastInlineEditJs =
         await File.ReadAllTextAsync(broadcastInlineEditJsPath);

      Assert.Contains(
         "window.initializeBroadcastInlineEditing =",
         siteJs
      );
      Assert.Contains(
         "window.initializeBroadcastInlineEditing?.(cell);",
         broadcastInlineEditJs
      );
   }
}
