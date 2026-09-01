namespace SESport.Core.Tests.Pages.Admin;

public sealed class AdminScriptCleanupTests
{
   [Fact]
   public async Task RemovedCheckboxContractsHaveNoRemainingCodeOrMarkup()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var scriptPaths = new[]
      {
         Path.Combine(
            repoRoot,
            "src/SESport.Web/wwwroot/Admin/js/admin-forms.js"
         ),
         Path.Combine(
            repoRoot,
            "src/SESport.Web/wwwroot/Admin/js/admin-shared.js"
         ),
         Path.Combine(
            repoRoot,
            "src/SESport.Web/wwwroot/Admin/js/site.js"
         )
      };
      var scripts = string.Join(
         Environment.NewLine,
         await Task.WhenAll(
            scriptPaths.Select(path => File.ReadAllTextAsync(path))
         )
      );
      var pagesRoot = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages"
      );
      var pageMarkup = string.Join(
         Environment.NewLine,
         Directory
            .EnumerateFiles(
               pagesRoot,
               "*.cshtml",
               SearchOption.AllDirectories
            )
            .Select(path => File.ReadAllText(path))
      );

      Assert.DoesNotContain("checkboxToggleSelector", scripts);
      Assert.DoesNotContain("checkboxVisibilitySelector", scripts);
      Assert.DoesNotContain("initializeCheckboxToggles", scripts);
      Assert.DoesNotContain("initializeCheckboxVisibility", scripts);
      Assert.DoesNotContain("refreshCheckboxControls", scripts);
      Assert.DoesNotContain("normalizeString", scripts);
      Assert.DoesNotContain("normalizeNullableString", scripts);
      Assert.DoesNotContain("data-checkbox-toggle", pageMarkup);
      Assert.DoesNotContain(
         "data-visible-when-checkbox-group",
         pageMarkup
      );
      Assert.DoesNotContain("data-checkbox-group", pageMarkup);
   }
}
