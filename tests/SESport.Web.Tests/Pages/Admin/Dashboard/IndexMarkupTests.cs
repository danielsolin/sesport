namespace SESport.Core.Tests.Pages.Admin.Dashboard;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task DashboardShowsActionableSections()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var pagePath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Dashboard/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(pagePath);

      Assert.Contains("Upcoming coverage", html);
      Assert.Contains("Activities needing attention", html);
      Assert.Contains("System health", html);
      Assert.Contains("/Admin/Broadcasts/Index", html);
      Assert.Contains("/Admin/Activities/Edit", html);
      Assert.DoesNotContain("bio", html, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("No direct source", html);
   }

   [Fact]
   public void SourceIssueLabelReflectsGroupScope()
   {
      var groupedIssue = CreateSourceIssue(hasNoGroup: false);
      var ungroupedIssue = CreateSourceIssue(hasNoGroup: true);

      var groupedLabels = SESport.Web.Pages.Admin.Dashboard.IndexModel
         .GetIssueLabels(groupedIssue);
      var ungroupedLabels = SESport.Web.Pages.Admin.Dashboard.IndexModel
         .GetIssueLabels(ungroupedIssue);

      Assert.Contains("No source in group", groupedLabels);
      Assert.Contains("No source", ungroupedLabels);
      Assert.DoesNotContain("No direct source", groupedLabels);
   }

   private static SESport.Data.DashboardActivityIssue CreateSourceIssue(
      bool hasNoGroup
   )
   {
      return new SESport.Data.DashboardActivityIssue(
         Guid.NewGuid(),
         new DateOnly(2199, 12, 1),
         "Test Activity",
         "Published",
         false,
         false,
         false,
         hasNoGroup,
         true
      );
   }
}
