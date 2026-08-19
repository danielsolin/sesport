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
      var workerPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/service-worker.js"
      );

      var page = await File.ReadAllTextAsync(pagePath);
      var model = await File.ReadAllTextAsync(modelPath);
      var script = await File.ReadAllTextAsync(scriptPath);
      var worker = await File.ReadAllTextAsync(workerPath);

      Assert.Contains("data-member-watch-push-configured", page);
      Assert.Contains("data-member-watch-vapid-public-key", page);
      Assert.Contains("data-member-watch-push-status", page);
      Assert.Contains("data-member-watch-push-activate", page);
      Assert.Contains("RegisterPush", page);
      Assert.Contains("SetNotificationLeadTime", page);
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
      Assert.Contains("Skicka notis", model);
      Assert.Contains(
         "MemberNotificationLeadTimes.SupportedMinutes",
         model
      );
      Assert.Contains("PushManager", script);
      Assert.Contains("service-worker.js", script);
      Assert.Contains("pushSubscription", script);
      Assert.Contains("showNotification", worker);
      Assert.Contains("notificationclick", worker);
   }
}
