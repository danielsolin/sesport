using SESport.Web.Configuration;

namespace SESport.Core.Tests.Configuration;

public sealed class WebStatsReportDirectoryResolverTests : IDisposable
{
   private readonly string testRoot = Path.Combine(
      Path.GetTempPath(),
      $"sesport-stats-resolver-{Guid.NewGuid():N}"
   );

   [Fact]
   public void ResolveFindsReportsInRepositoryParent()
   {
      var contentRoot = Path.Combine(testRoot, "src", "SESport.Web");
      var reportDirectory = Path.Combine(testRoot, "data", "web-stats");
      Directory.CreateDirectory(contentRoot);
      Directory.CreateDirectory(reportDirectory);

      var result = WebStatsReportDirectoryResolver.Resolve(
         string.Empty,
         contentRoot,
         Path.Combine(contentRoot, "bin")
      );

      Assert.Equal(reportDirectory, result);
   }

   [Fact]
   public void ResolveUsesReportsShippedWithApplicationAsFallback()
   {
      var contentRoot = Path.Combine(testRoot, "content");
      var applicationDirectory = Path.Combine(testRoot, "published");
      Directory.CreateDirectory(contentRoot);

      var result = WebStatsReportDirectoryResolver.Resolve(
         string.Empty,
         contentRoot,
         applicationDirectory
      );

      Assert.Equal(
         Path.Combine(applicationDirectory, "data", "web-stats"),
         result
      );
   }

   [Fact]
   public void ResolveUsesConfiguredDirectory()
   {
      var contentRoot = Path.Combine(testRoot, "content");
      Directory.CreateDirectory(contentRoot);

      var result = WebStatsReportDirectoryResolver.Resolve(
         "custom-stats",
         contentRoot,
         Path.Combine(testRoot, "published")
      );

      Assert.Equal(
         Path.Combine(contentRoot, "custom-stats"),
         result
      );
   }

   public void Dispose()
   {
      if(Directory.Exists(testRoot))
      {
         Directory.Delete(testRoot, recursive: true);
      }
   }
}
