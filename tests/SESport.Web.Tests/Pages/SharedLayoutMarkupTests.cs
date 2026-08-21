namespace SESport.Core.Tests.Pages;

public sealed class SharedLayoutMarkupTests
{
   [Fact]
   public async Task SharedLayoutSeparatesPublicAndAdminAssets()
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
      var indexPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Index.cshtml"
      );
      var index = await File.ReadAllTextAsync(indexPath);
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
      var adminSiteJsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/Admin/js/site.js"
      );
      var adminSiteJs = await File.ReadAllTextAsync(adminSiteJsPath);

      Assert.Contains("public.css", html);
      Assert.Contains("site.css", html);
      Assert.Contains(
         "src=\"~/Admin/js/site.js\"",
         html
      );
      Assert.Contains(
         "src=\"~/js/site.js\"",
         html
      );
      Assert.Contains(
         "~/Admin/vendor/multi-select-dropdown-js/"
            + "MultiSelect.min.css",
         html
      );
      Assert.Contains(
         "~/Admin/vendor/multi-select-dropdown-js/"
            + "MultiSelect.min.js",
         html
      );
      Assert.DoesNotContain(
         "~/vendor/multi-select-dropdown-js/",
         html
      );
      Assert.Contains("!isAdmin", html);
      Assert.Contains("var broadcastsHref = Url.Page(", html);
      Assert.Contains("var activitiesHref = Url.Page(", html);
      Assert.Contains("var dashboardHref = Url.Page(", html);
      Assert.Contains("Dashboard", html);
      Assert.DoesNotContain("shareSelectedDate", html);
      Assert.DoesNotContain("dateRouteValues", html);
      Assert.DoesNotContain("Bli medlem", html);
      Assert.DoesNotContain("public-contact-link", html);
      Assert.DoesNotContain("public-member-label", html);
      Assert.DoesNotContain("public-member-button", html);
      Assert.DoesNotContain("public-member-form", html);
      Assert.DoesNotContain("Account/Logout", html);
      Assert.Contains("class=\"public-header-actions\"", html);
      Assert.Contains("class=\"@publicLoginClass\"", html);
      Assert.Contains("var isPublicStatistics = requestPath.Equals(", html);
      Assert.Contains("var isPublicWatches = requestPath.Equals(", html);
      Assert.Contains("var isPublicLogin = requestPath.Equals(", html);
      Assert.Contains("var publicStatisticsClass = isPublicStatistics", html);
      Assert.Contains("var publicWatchesClass = isPublicWatches", html);
      Assert.Contains("var publicLoginClass = isPublicLogin", html);
      Assert.Contains("isPublicStatistics ? \"page\" : null", html);
      Assert.Contains("isPublicWatches ? \"page\" : null", html);
      Assert.Contains("isPublicLogin ? \"page\" : null", html);
      Assert.Contains(
         "PublicFilterPreferenceStore.ReadQueryString",
         html
      );
      Assert.Contains("publicIndexHref", html);
      Assert.Contains("href=\"@publicIndexHref\"", html);
      Assert.Contains("asp-page=\"/Account/Login\"", html);
      Assert.Contains("Logga in", html);
      Assert.Contains("BEVAKNINGAR", html);
      Assert.DoesNotContain("Logga ut", html);
      Assert.Contains("class=\"public-contact-footer\"", index);
      Assert.Contains("class=\"public-contact-link\"", index);
      Assert.Contains("href=\"mailto:info@sesport.se\"", index);
      Assert.Contains(".public-contact-footer {", publicCss);
      Assert.Contains(".public-contact-link {", publicCss);
      Assert.Contains(
         ".page-shell {\n" +
            "   padding-top: 24px;\n" +
            "   padding-bottom: 24px;",
         publicCss
      );
      Assert.Contains(
         "width: calc(\n" +
            "      100% -\n" +
            "      var(--activity-card-center-offset) -\n" +
            "      var(--activity-card-center-offset)",
         publicCss
      );
      Assert.Contains(
         "margin: 64px 0 0\n" +
            "      calc(\n" +
            "         var(--activity-card-center-offset) +\n" +
            "         var(--activity-card-center-offset)",
         publicCss
      );
      Assert.Contains("padding-bottom: 0;", publicCss);
      Assert.Contains(
         "--activity-card-center-offset: 94px;",
         publicCss
      );
      Assert.DoesNotContain(
         "transform: translateX(var(--activity-card-center-offset));",
         publicCss
      );
      Assert.Contains(
         "--activity-card-center-offset: 64px;",
         publicCss
      );
      Assert.Contains(".public-member-link {", publicCss);
      Assert.Contains("display: inline-flex;", publicCss);
      Assert.Contains("border-radius: 999px;", publicCss);
      Assert.Contains("text-transform: uppercase;", publicCss);
      Assert.Contains(".public-member-link.is-active {", publicCss);
      Assert.Contains(
         "const isRootPath = currentPath === \"/\";\n" +
         "   const isDesktopDevice = !isMobileDevice();\n\n" +
         "   if(isRootPath && !isDesktopDevice)",
         siteJs
      );
      Assert.Contains("function isMobileDevice()", siteJs);
      Assert.Contains(
         "window.navigator.userAgentData?.mobile",
         siteJs
      );
      Assert.Contains(
         "window.submitFilterForm = submitFilterForm;",
         adminSiteJs
      );
      Assert.Contains(
         "initializeBroadcastInlineEditing",
         adminSiteJs
      );
      Assert.DoesNotContain(
         "sesport-public-auto-reload",
         adminSiteJs
      );
      Assert.DoesNotContain(
         "initializeBroadcastInlineEditing",
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
