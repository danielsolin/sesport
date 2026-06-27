namespace SESport.Core.Tests.Pages;

public sealed class SharedLayoutMarkupTests
{
   [Fact]
   public async Task SharedLayoutLoadsPublicCssOnlyForNonAdminPages()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var layoutPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Shared/_Layout.cshtml"
      );
      var html = await File.ReadAllTextAsync(layoutPath);

      Assert.Contains("public.css", html);
      Assert.Contains("site.css", html);
      Assert.Contains("!isAdmin", html);
      Assert.Contains("var broadcastsHref = Url.Page(", html);
      Assert.Contains("var activitiesHref = Url.Page(", html);
      Assert.Contains("RouteKeys.Date", html);
   }
}
