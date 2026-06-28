namespace SESport.Core.Tests.Pages;

public sealed class CurrentMarkerScrollTests
{
   [Fact]
   public async Task SiteJsCentersCurrentMarkerScroll()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var jsPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/site.js"
      );
      var js = await File.ReadAllTextAsync(jsPath);

      Assert.Contains("currentMarkerSelector", js);
      Assert.Contains("block: \"center\"", js);
      Assert.DoesNotContain("block: \"start\"", js);
   }
}
