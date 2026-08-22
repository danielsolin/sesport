namespace SESport.Core.Tests.Pages.Account;

public sealed class WatchesMarkupTests
{
   [Fact]
   public async Task WatchesPageIncludesPushAndLeadTimeHooks()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var pagePath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Account/Watches.cshtml"
      );
      var modelPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Account/Watches.cshtml.cs"
      );
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/member-watches.js"
      );
      var searchResultsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Account/_WatchSearchResults.cshtml"
      );
      var avatarPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Account/_WatchPersonAvatar.cshtml"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/public.css"
      );
      var workerPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/service-worker.js"
      );

      var page = await File.ReadAllTextAsync(pagePath);
      var model = await File.ReadAllTextAsync(modelPath);
      var script = await File.ReadAllTextAsync(scriptPath);
      var searchResults = await File.ReadAllTextAsync(searchResultsPath);
      var avatar = await File.ReadAllTextAsync(avatarPath);
      var css = await File.ReadAllTextAsync(cssPath);
      var worker = await File.ReadAllTextAsync(workerPath);

      var titleIndex = page.IndexOf(
         "<h1 class=\"member-watches-section-title\">BEVAKNINGAR</h1>",
         StringComparison.Ordinal
      );
      var searchIndex = page.IndexOf(
         "<div class=\"member-watches-search-container\"",
         StringComparison.Ordinal
      );
      Assert.DoesNotContain("LÄGG TILL", page);
      Assert.True(titleIndex >= 0);
      Assert.True(searchIndex > titleIndex);
      Assert.DoesNotContain("member-watches-sort-settings", page);
      Assert.DoesNotContain("SortQueryParameter", model);
      Assert.DoesNotContain("SortOptions", model);
      Assert.Contains("data-member-watch-push-configured", page);
      Assert.Contains("data-member-watch-vapid-public-key", page);
      Assert.Contains("data-member-watch-push-status", page);
      Assert.Contains("data-member-watch-push-activate", page);
      Assert.Contains("RegisterPush", page);
      Assert.Contains("SetNotificationLeadTime", page);
      Assert.Contains(
         "data-member-watch-auto-submit-form",
         page
      );
      Assert.Contains("NextActivity", page);
      Assert.Contains(
         "FormatLocalTimestampWithoutSeconds",
         page
      );
      Assert.Contains("NÄSTA:", page);
      Assert.Contains("RelatedOrganizationName", page);
      Assert.Contains("member-watch-next-activity", page);
      Assert.Contains(
         "PartialAsync(\"_WatchPersonAvatar\", watch)",
         page
      );
      Assert.Contains("data-member-watch-add-row", searchResults);
      Assert.Contains("member-watch-search-result", searchResults);
      Assert.Contains("IsWatched", searchResults);
      Assert.Contains("Redan tillagd", searchResults);
      Assert.Contains(
         "member-watch-already-added-label",
         searchResults
      );
      Assert.Contains(
         "PartialAsync(\"_WatchPersonAvatar\", person,",
         searchResults
      );
      Assert.Contains(
         "new ViewDataDictionary(ViewData)",
         searchResults
      );
      Assert.Contains("DisableImageSourceLink", searchResults);
      Assert.Contains("HasPrimaryImage", avatar);
      Assert.Contains("PrimaryImageSource", avatar);
      Assert.Contains("disableImageSourceLink", avatar);
      Assert.Contains("member-watch-avatar", avatar);
      Assert.Contains("member-watch-image", avatar);
      Assert.Contains("member-watch-image-link", avatar);
      Assert.Contains("member-watch-image-tooltip", avatar);
      Assert.Contains("noopener noreferrer", avatar);
      Assert.Contains("loading=\"lazy\"", avatar);
      Assert.Contains("decoding=\"async\"", avatar);
      Assert.Contains("viewBox=\"0 0 24 24\"", avatar);
      Assert.DoesNotContain("autofocus", page);
      Assert.Contains(
         "aria-label=\"När ska notisen skickas?\"",
         page
      );
      Assert.Contains(
         "data-member-watch-notification-form",
         page
      );
      Assert.Contains("pushSubscription", model);
      Assert.Contains("OnPostRegisterPushAsync", model);
      Assert.Contains("OnGetImageAsync", model);
      Assert.Contains(
         "MemberWatchDefaults.MinimumSearchLength",
         model
      );
      Assert.Contains(
         "MemberWatchDefaults.MaxSearchResults",
         model
      );
      Assert.DoesNotContain(
         "private const int MaxSearchResults",
         model
      );
      Assert.DoesNotContain(
         "private const int MinimumSearchLength",
         model
      );
      Assert.Contains(
         "GetPersonPrimaryImageAsync",
         model
      );
      Assert.Contains("Skicka notis", model);
      Assert.Contains(
         "MemberNotificationLeadTimes.SupportedMinutes",
         model
      );
      Assert.Contains("autoSubmitFormSelector", script);
      Assert.Contains("PushManager", script);
      Assert.Contains("requestPermission", script);
      Assert.Contains("push service error", script);
      Assert.Contains("service-worker.js", script);
      Assert.Contains("pushSubscription", script);
      Assert.Contains("data-member-watch-add-row", script);
      Assert.Contains("requestSubmit", script);
      Assert.Contains("target.closest(", script);
      Assert.Contains("minimumSearchLength = 2", script);
      Assert.Contains("debounceMs = 300", script);
      Assert.Contains(
         "query.length < minimumSearchLength",
         script
      );
      Assert.Contains("minlength=\"2\"", page);
      Assert.Contains(
         ".member-watches-push-status.is-active {",
         css
      );
      Assert.Contains("background: #f4faf4;", css);
      Assert.Contains("background: var(--subgrid-row);", css);
      Assert.Contains("max-height: 420px;", css);
      Assert.Contains("border-left: 3px solid", css);
      Assert.Contains("gap: 6px;", css);
      Assert.Contains(".member-watch-avatar {", css);
      Assert.Contains("flex: 0 0 40px;", css);
      Assert.Contains("color: #ffcc00;", css);
      Assert.Contains("opacity: 0.65;", css);
      Assert.Contains("overflow: hidden;", css);
      Assert.Contains(".member-watch-avatar img {", css);
      Assert.Contains("object-fit: cover;", css);
      Assert.Contains("object-position: center top;", css);
      Assert.Contains(".member-watch-image-container {", css);
      Assert.Contains(":focus-within", css);
      Assert.Contains("padding: 12px 12px 12px 8px;", css);
      Assert.Contains(
         ".member-watch-next-activity {",
         css
      );
      Assert.Contains("font-size: 10px;", css);
      Assert.Contains("padding: 8px 8px 8px 0;", css);
      Assert.Contains("padding: 12px 16px 12px 12px;", css);
      Assert.Contains(
         "@media (hover: hover) and (pointer: fine)",
         css
      );
      Assert.Contains(
         ".member-watch-result[data-member-watch-add-row]:hover",
         css
      );
      Assert.Contains(
         ".member-watch-already-added-label {",
         css
      );
      Assert.Contains("showNotification", worker);
      Assert.Contains("notificationclick", worker);
   }
}
