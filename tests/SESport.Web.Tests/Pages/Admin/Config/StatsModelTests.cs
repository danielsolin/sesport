using SESport.Core.Configuration;
using SESport.Web.Pages.Admin.Config;

namespace SESport.Core.Tests.Pages.Admin.Config;

public sealed class StatsModelTests : IDisposable
{
   private readonly string reportDirectory = Path.Combine(
      Path.GetTempPath(),
      $"sesport-stats-{Guid.NewGuid():N}"
   );

   [Fact]
   public void OnGetListsOnlySupportedReportNames()
   {
      Directory.CreateDirectory(reportDirectory);
      File.WriteAllText(
         Path.Combine(reportDirectory, "latest.html"),
         "latest"
      );
      File.WriteAllText(
         Path.Combine(reportDirectory, "2026-07-24.html"),
         "dated"
      );
      File.WriteAllText(
         Path.Combine(reportDirectory, "unexpected.html"),
         "unexpected"
      );
      var model = CreateModel();

      model.OnGet(null);

      Assert.Collection(
         model.Reports,
         report => Assert.Equal("latest.html", report.FileName),
         report => Assert.Equal("2026-07-24.html", report.FileName)
      );
      Assert.Equal("latest.html", model.SelectedReport?.FileName);
   }

   [Fact]
   public void OnGetReportRejectsFilesOutsideSupportedReports()
   {
      Directory.CreateDirectory(reportDirectory);
      var model = CreateModel();

      var result = model.OnGetReport("../.env");

      Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundResult>(result);
   }

   public void Dispose()
   {
      if(Directory.Exists(reportDirectory))
      {
         Directory.Delete(reportDirectory, recursive: true);
      }
   }

   private StatsModel CreateModel()
   {
      return new StatsModel(
         new WebStatsOptions
         {
            ReportDirectory = reportDirectory
         }
      );
   }
}
