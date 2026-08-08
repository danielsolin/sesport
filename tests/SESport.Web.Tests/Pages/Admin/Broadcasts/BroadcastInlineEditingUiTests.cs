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
      var groupAutocompleteJsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/"
            + "broadcast-activity-group-autocomplete.js"
      );
      var layoutPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Shared/_Layout.cshtml"
      );
      var siteJs = await File.ReadAllTextAsync(siteJsPath);
      var broadcastInlineEditJs =
         await File.ReadAllTextAsync(broadcastInlineEditJsPath);
      var groupAutocompleteJs =
         await File.ReadAllTextAsync(groupAutocompleteJsPath);
      var layout = await File.ReadAllTextAsync(layoutPath);

      Assert.Contains(
         "window.initializeBroadcastInlineEditing =",
         siteJs
      );
      Assert.Contains(
         "descriptionEditor.dataset.broadcastInlineEditField",
         siteJs
      );
      Assert.Contains(
         "window.initializeBroadcastInlineEditing?.(cell);",
         broadcastInlineEditJs
      );
      Assert.Contains("organizationEntityId", groupAutocompleteJs);
      Assert.Contains(
         "broadcast-activity-group-suggestions-fixed",
         groupAutocompleteJs
      );
      Assert.Contains(
         "document.addEventListener(\"focusin\"",
         groupAutocompleteJs
      );
      Assert.Contains(
         "document.addEventListener(\"input\"",
         groupAutocompleteJs
      );
      Assert.Contains("activityGroupId", broadcastInlineEditJs);
      Assert.Contains(
         "broadcastInlineEditDescriptionField = \"description\"",
         broadcastInlineEditJs
      );
      Assert.Contains(
         "broadcastDescriptionTextSelector",
         broadcastInlineEditJs
      );
      Assert.Contains(
         "data-broadcast-description-text",
         broadcastInlineEditJs
      );
      Assert.Contains(
         "broadcast-activity-group-autocomplete.js",
         layout
      );
   }
}
