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
         "src/SESport.Web/wwwroot/Admin/js/site.js"
      );
      var broadcastInlineEditJsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/Admin/js/"
            + "broadcast-inline-edit.js"
      );
      var groupAutocompleteJsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/Admin/js/"
            + "broadcast-activity-group-autocomplete.js"
      );
      var organizationAutocompleteJsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/Admin/js/"
            + "broadcast-organization-autocomplete.js"
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
      var organizationAutocompleteJs =
         await File.ReadAllTextAsync(organizationAutocompleteJsPath);
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
         "function formatOrgSearchResult(item)",
         organizationAutocompleteJs
      );
      Assert.Contains("item.sport", organizationAutocompleteJs);
      Assert.Contains(
         "`${item.text} (${item.sport})`",
         organizationAutocompleteJs
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
         "broadcastInlineEditChannelField = \"channel\"",
         broadcastInlineEditJs
      );
      Assert.Contains(
         "broadcastInlineEditStartTimeField = \"start-time\"",
         broadcastInlineEditJs
      );
      Assert.Contains(
         "broadcastInlineEditEndTimeField = \"end-time\"",
         broadcastInlineEditJs
      );
      Assert.Contains(
         "broadcastDescriptionTextSelector",
         broadcastInlineEditJs
      );
      Assert.Contains("Add description..", broadcastInlineEditJs);
      Assert.Contains("Add categories..", broadcastInlineEditJs);
      Assert.Contains("inline-edit-placeholder", broadcastInlineEditJs);
      Assert.Contains(
         "data-broadcast-description-text",
         broadcastInlineEditJs
      );
      Assert.Contains(
         "src=\"~/Admin/js/broadcast-inline-edit.js\"",
         layout
      );
      Assert.Contains(
         "src=\"~/Admin/js/broadcast-organization-autocomplete.js\"",
         layout
      );
      Assert.Contains(
         "src=\"~/Admin/js/broadcast-activity-group-autocomplete.js\"",
         layout
      );
      Assert.DoesNotContain(
         "src=\"~/js/broadcast-inline-edit.js\"",
         layout
      );
      Assert.DoesNotContain(
         "src=\"~/js/broadcast-organization-autocomplete.js\"",
         layout
      );
      Assert.DoesNotContain(
         "src=\"~/js/broadcast-activity-group-autocomplete.js\"",
         layout
      );
   }
}
