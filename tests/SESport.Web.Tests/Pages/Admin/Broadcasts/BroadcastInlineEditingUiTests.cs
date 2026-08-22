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
      var rowPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Broadcasts/_BroadcastRow.cshtml"
      );
      var organizationSuggestionsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Ajax/Search/_OrganizationSuggestions.cshtml"
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
      var row = await File.ReadAllTextAsync(rowPath);
      var organizationSuggestions =
         await File.ReadAllTextAsync(organizationSuggestionsPath);
      var layout = await File.ReadAllTextAsync(layoutPath);

      Assert.Contains(
         "window.initializeBroadcastInlineEditing =",
         siteJs
      );
      Assert.Contains("replaceElementWithPartialHtml", siteJs);
      Assert.Contains("loadPartialAsync", broadcastInlineEditJs);
      Assert.Contains("organizationEntityId", groupAutocompleteJs);
      Assert.Contains(
         "broadcast-activity-group-suggestions-fixed",
         groupAutocompleteJs
      );
      Assert.Contains("format\", \"organization-suggestions\"", organizationAutocompleteJs);
      Assert.Contains("replaceContentsWithPartialHtml", organizationAutocompleteJs);
      Assert.Contains("entity.Sport", organizationSuggestions);
      Assert.Contains("data-broadcast-inline-edit-field=\"channel\"", row);
      Assert.Contains("data-broadcast-categories-list", row);
      Assert.Contains(
         "document.addEventListener(\"focusin\"",
         groupAutocompleteJs
      );
      Assert.Contains(
         "document.addEventListener(\"input\"",
         groupAutocompleteJs
      );
      Assert.Contains(
         "document.addEventListener(\"DOMContentLoaded\", () =>",
         groupAutocompleteJs
      );
      Assert.Contains("root instanceof Document", groupAutocompleteJs);
      Assert.Contains("activityGroupId", broadcastInlineEditJs);
      Assert.Contains("description: \"Add description..\"", broadcastInlineEditJs);
      Assert.Contains("channel: \"Add channel..\"", broadcastInlineEditJs);
      Assert.Contains("\"start-time\": \"Add start time..\"", broadcastInlineEditJs);
      Assert.Contains("\"end-time\": \"Add end time..\"", broadcastInlineEditJs);
      Assert.Contains("Add description..", broadcastInlineEditJs);
      Assert.Contains("Add categories..", broadcastInlineEditJs);
      Assert.DoesNotContain("createElement", broadcastInlineEditJs);
      Assert.DoesNotContain("innerHTML", broadcastInlineEditJs);
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
