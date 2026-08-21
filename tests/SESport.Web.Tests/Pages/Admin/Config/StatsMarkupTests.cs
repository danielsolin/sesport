namespace SESport.Core.Tests.Pages.Admin.Config;

public sealed class StatsMarkupTests
{
   [Fact]
   public async Task StatsPageUsesAdminDatePickerInsteadOfDateChips()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Config/Stats.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains(
         "<form method=\"get\" class=\"filter-form stats-report-picker\">",
         html
      );
      Assert.Contains("<input type=\"date\"", html);
      Assert.Contains("name=\"@RouteKeys.Date\"", html);
      Assert.Contains("onchange=\"this.form.submit();\"", html);
      Assert.DoesNotContain("stats-report-list", html);
      Assert.DoesNotContain("<h2>Web statistics</h2>", html);
      Assert.DoesNotContain(
         "Anonymized statistics generated from the Caddy access log.",
         html
      );
      Assert.Contains("stats-report-frame", html);
   }
}
