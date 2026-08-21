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
      var css = await File.ReadAllTextAsync(cssPath);
      var worker = await File.ReadAllTextAsync(workerPath);

      Assert.Contains("data-member-watch-push-configured", page);
      Assert.Contains("data-member-watch-vapid-public-key", page);
      Assert.Contains("data-member-watch-push-status", page);
      Assert.Contains("data-member-watch-push-activate", page);
      Assert.Contains("RegisterPush", page);
      Assert.Contains("SetNotificationLeadTime", page);
      Assert.Contains("member-watches-sort-settings", page);
      Assert.Contains(
         "data-member-watch-auto-submit-form",
         page
      );
      Assert.Contains("SortQueryParameter", page);
      Assert.Contains("NextActivity", page);
      Assert.Contains(
         "FormatLocalTimestampWithoutSeconds",
         page
      );
      Assert.Contains("NÄSTA:", page);
      Assert.Contains("RelatedOrganizationName", page);
      Assert.Contains("member-watch-next-activity", page);
      Assert.Contains("member-watch-avatar", page);
      Assert.Contains("HasPrimaryImage", page);
      Assert.Contains("member-watch-image", page);
      Assert.Contains("viewBox=\"0 0 24 24\"", page);
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
         "GetWatchedEntityPrimaryImageAsync",
         model
      );
      Assert.Contains("Skicka notis", model);
      Assert.Contains("Sortering: Namn", model);
      Assert.Contains("Sortering: Notis", model);
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
      Assert.Contains(
         ".member-watches-push-status.is-active {",
         css
      );
      Assert.Contains("background: #f4faf4;", css);
      Assert.Contains("background: var(--subgrid-row);", css);
      Assert.Contains("border-left: 3px solid", css);
      Assert.Contains("gap: 6px;", css);
      Assert.Contains(".member-watch-avatar {", css);
      Assert.Contains("flex: 0 0 40px;", css);
      Assert.Contains("color: #ffcc00;", css);
      Assert.Contains("opacity: 0.65;", css);
      Assert.Contains("overflow: hidden;", css);
      Assert.Contains(".member-watch-avatar img {", css);
      Assert.Contains("object-fit: cover;", css);
      Assert.Contains("padding: 12px 12px 12px 8px;", css);
      Assert.Contains(
         ".member-watch-next-activity {",
         css
      );
      Assert.Contains("font-size: 10px;", css);
      Assert.Contains("padding: 8px 8px 8px 0;", css);
      Assert.Contains("padding: 12px 16px 12px 12px;", css);
      Assert.Contains("showNotification", worker);
      Assert.Contains("notificationclick", worker);
   }
}
