namespace SESport.Core.Tests.Pages.Admin.Members;

public sealed class DetailsMarkupTests
{
   [Fact]
   public async Task DetailsPageIncludesMemberSummaryAndWatches()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Members/Details.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("@page \"{id:guid}\"", html);
      Assert.Contains("@Model.Member?.Email", html);
      Assert.Contains("Model.Member.CreatedAt", html);
      Assert.Contains("Model.Member.LastLoginAt", html);
      Assert.Contains("Model.Member.WatchCount", html);
      Assert.Contains("Model.Member.PushNotificationSentCount", html);
      Assert.Contains("Model.Member.LoginTokenCreatedCount", html);
      Assert.Contains("Model.Member.LoginTokenConsumedCount", html);
      Assert.Contains("Model.Watches.Count", html);
      Assert.Contains("@watch.Name", html);
      Assert.Contains("@watch.SportName", html);
      Assert.Contains("@watch.RelatedNames", html);
      Assert.Contains("watch.NextActivity", html);
      Assert.Contains("asp-page=\"./Index\"", html);
   }
}
