namespace SESport.Core.Tests.Pages.Admin.Members;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task IndexPageExposesMemberInformation()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Members/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("admin-table admin-table-hover members-table", html);
      Assert.Contains("@member.Email", html);
      Assert.Contains("member.CreatedAt", html);
      Assert.Contains("member.LastLoginAt", html);
      Assert.Contains("@member.WatchCount", html);
      Assert.Contains("member.PushNotificationSentCount", html);
      Assert.Contains("member.LoginTokenCreatedCount", html);
      Assert.Contains("member.LoginTokenConsumedCount", html);
      Assert.Contains(">Watches<", html);
      Assert.Contains(">Push sent<", html);
      Assert.Contains(">Logins created<", html);
      Assert.Contains(">Logins used<", html);
      Assert.DoesNotContain("<p", html);
   }
}
