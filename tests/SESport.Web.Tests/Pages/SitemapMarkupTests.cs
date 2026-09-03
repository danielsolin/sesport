namespace SESport.Core.Tests.Pages;

public sealed class SitemapMarkupTests
{
   [Fact]
   public async Task SitemapIsAnXmlPageWithCanonicalPublicRoutes()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var page = await File.ReadAllTextAsync(
         Path.Combine(repoRoot, "src/SESport.Web/Pages/Sitemap.cshtml")
      );
      var robots = await File.ReadAllTextAsync(
         Path.Combine(repoRoot, "src/SESport.Web/wwwroot/robots.txt")
      );

      Assert.Contains("@page \"/sitemap.xml\"", page);
      Assert.Contains("Layout = null", page);
      Assert.Contains("sitemaps.org/schemas/sitemap/0.9", page);
      Assert.Contains("@url", page);
      Assert.Contains("User-agent: *", robots);
      Assert.Contains("Allow: /", robots);
      Assert.Contains(
         "Sitemap: https://sesport.se/sitemap.xml",
         robots
      );
   }
}
