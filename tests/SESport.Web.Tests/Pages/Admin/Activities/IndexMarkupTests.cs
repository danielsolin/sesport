namespace SESport.Core.Tests.Pages.Admin.Activities;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task IndexPageShowsActivityGroupColumn()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Activities/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var timeHeaderIndex = html.IndexOf(
         "Time @Model.GetSortIndicator"
      );
      var groupHeaderIndex = html.IndexOf(
         "Group @Model.GetSortIndicator"
      );
      var activityHeaderIndex = html.IndexOf(
         "Activity @Model.GetSortIndicator"
      );

      Assert.True(timeHeaderIndex < activityHeaderIndex);
      Assert.True(activityHeaderIndex < groupHeaderIndex);
      Assert.Contains(
         "@Model.GetSortRouteValues(\"Time\")",
         html
      );
      Assert.Contains(
         "data-ajax-count-target=\"[data-activity-count]\"",
         html
      );
      Assert.Contains(
         "data-ajax-count-value=\"@Model.Activities.Count\"",
         html
      );
      Assert.Contains("filter-form-count", html);
      Assert.Contains("Activities:", html);
      Assert.Contains("data-activity-count", html);
      Assert.Contains("@activity.ActivityGroupTitle", html);
      Assert.Contains(
         "@Model.GetSortRouteValues(\"Group\")",
         html
      );
      Assert.Contains("activities-table-actions", html);
      Assert.Contains("table-actions-stack", html);
      Assert.Contains("Find Facts", html);
      Assert.Contains("Find Start", html);
      Assert.Contains("Find Result", html);
   }
}
