namespace SESport.Core.Tests.Pages.Admin.ActivityGroups;

public sealed class EditMarkupTests
{
   [Fact]
   public async Task EditPageIncludesNoGroupingSetting()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/ActivityGroups/Edit.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains(
         "asp-for=\"ActivityGroup.NoGrouping\"",
         html
      );
      Assert.Contains("No Grouping", html);
      Assert.Contains(
         "asp-for=\"ActivityGroup.PublicDateMode\"",
         html
      );
      Assert.Contains("Public date", html);
   }
}
