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
      var publicCssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/public.css"
      );
      var html = await File.ReadAllTextAsync(layoutPath);
      var publicCss = await File.ReadAllTextAsync(publicCssPath);
      var siteCssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var siteCss = await File.ReadAllTextAsync(siteCssPath);
      var siteJsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/site.js"
      );
      var siteJs = await File.ReadAllTextAsync(siteJsPath);

      Assert.Contains("public.css", html);
      Assert.Contains("site.css", html);
      Assert.Contains("!isAdmin", html);
      Assert.Contains("var broadcastsHref = Url.Page(", html);
      Assert.Contains("var activitiesHref = Url.Page(", html);
      Assert.Contains("var dashboardHref = Url.Page(", html);
      Assert.Contains("Dashboard", html);
      Assert.DoesNotContain("shareSelectedDate", html);
      Assert.DoesNotContain("dateRouteValues", html);
      Assert.DoesNotContain("public-member-link", html);
      Assert.DoesNotContain("Bli medlem", html);
      Assert.Contains("class=\"public-contact-link\"", html);
      Assert.Contains(".public-contact-link {", publicCss);
      Assert.Contains(
         "const isRootPath = currentPath === \"/\";\n\n" +
         "   if(isRootPath)",
         siteJs
      );
      Assert.DoesNotContain(".activity-entry-title {", siteCss);
      Assert.DoesNotContain(
         ".activity-entry-organization {",
         siteCss
      );
      Assert.Contains("justify-content: space-between", publicCss);
      Assert.Contains("width: 100%", publicCss);
      Assert.True(
         html.IndexOf(
            "asp-page=\"/Admin/Entities/Index\"",
            StringComparison.Ordinal
         ) < html.IndexOf(
            "asp-page=\"/Admin/Runs/Index\"",
            StringComparison.Ordinal
         )
      );
   }
}
