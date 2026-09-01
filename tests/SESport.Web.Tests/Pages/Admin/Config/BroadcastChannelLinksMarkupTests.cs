namespace SESport.Core.Tests.Pages.Admin.Config;

public sealed class BroadcastChannelLinksMarkupTests
{
   [Fact]
   public async Task ChannelLinkPagesPreserveStateAndValidateNames()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var pageRoot = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Config/BroadcastChannelLinks"
      );
      var editModel = await File.ReadAllTextAsync(
         Path.Combine(pageRoot, "Edit.cshtml.cs")
      );
      var editPage = await File.ReadAllTextAsync(
         Path.Combine(pageRoot, "Edit.cshtml")
      );
      var indexPage = await File.ReadAllTextAsync(
         Path.Combine(pageRoot, "Index.cshtml")
      );
      var navigation = await File.ReadAllTextAsync(
         Path.Combine(
            repoRoot,
            "src/SESport.Web/Navigation/AdminNavigationBuilder.cs"
         )
      );

      Assert.Contains("IsActive = existing.IsActive", editModel);
      Assert.Contains("Link.OriginalCanonicalName", editModel);
      Assert.Contains("ValidateUniqueNamesAsync", editModel);
      Assert.Contains("asp-for=\"Link.IsActive\"", editPage);
      Assert.Contains(
         "Changes are loaded when the server starts.",
         editPage
      );
      Assert.Contains("Restart the server after", editPage);
      Assert.Contains("Broadcast Channel Links", indexPage);
      Assert.Contains(
         "/Admin/Config/BroadcastChannelLinks",
         navigation
      );
   }
}
