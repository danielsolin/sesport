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
      Assert.Contains("@activity.ActivityGroupTitle", html);
      Assert.Contains(
         "@Model.GetSortRouteValues(\"Group\")",
         html
      );
   }
}
