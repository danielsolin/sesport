using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.Formatting;
using System.Globalization;

namespace SESport.Web.Pages.Admin.Config;

public sealed class StatsModel(WebStatsOptions options) : PageModel
{
   private const string ReportDateFormat = DateDisplay.DateOnlyFormat;

   public IReadOnlyList<WebStatsReport> Reports { get; private set; } = [];

   public WebStatsReport? SelectedReport { get; private set; }

   public DateOnly? SelectedDate { get; private set; }

   public string SelectedDateText => SelectedDate?.ToString(
      ReportDateFormat,
      CultureInfo.InvariantCulture
   ) ?? string.Empty;

   public IActionResult OnGet(string? report, DateOnly? date)
   {
      Reports = GetReports();

      if(date.HasValue)
      {
         SelectedDate = date;
         SelectedReport = Reports.FirstOrDefault(item =>
            item.Date == date
         );
      }
      else
      {
         SelectedReport = Reports.FirstOrDefault(item =>
            string.Equals(
               item.FileName,
               report,
               StringComparison.Ordinal
            )
         ) ?? Reports.FirstOrDefault();
         SelectedDate = SelectedReport?.Date ?? Reports
            .FirstOrDefault(item => item.Date.HasValue)?.Date;
      }

      return Page();
   }

   public IActionResult OnGetReport(string file)
   {
      var report = GetReports().FirstOrDefault(item =>
         string.Equals(
            item.FileName,
            file,
            StringComparison.Ordinal
         )
      );

      if(report is null)
      {
         return NotFound();
      }

      return PhysicalFile(report.Path, "text/html; charset=utf-8");
   }

   private IReadOnlyList<WebStatsReport> GetReports()
   {
      if(
         string.IsNullOrWhiteSpace(options.ReportDirectory) ||
         !Directory.Exists(options.ReportDirectory)
      )
      {
         return [];
      }

      return Directory
         .EnumerateFiles(
            options.ReportDirectory,
            "*.html",
            SearchOption.TopDirectoryOnly
         )
         .Select(CreateReport)
         .Where(report => report is not null)
         .Cast<WebStatsReport>()
         .OrderBy(
            report => report.FileName ==
               WebStatsDefaults.LatestReportFileName ? 0 : 1
         )
         .ThenByDescending(report => report.FileName)
         .ToList();
   }

   private static WebStatsReport? CreateReport(string path)
   {
      var fileName = Path.GetFileName(path);

      if(
         string.Equals(
            fileName,
            WebStatsDefaults.LatestReportFileName,
            StringComparison.Ordinal
         )
      )
      {
         return new WebStatsReport(fileName, "Latest", path);
      }

      var name = Path.GetFileNameWithoutExtension(fileName);
      if(
         !DateOnly.TryParseExact(
            name,
            ReportDateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date
         )
      )
      {
         return null;
      }

      return new WebStatsReport(
         fileName,
         date.ToString(ReportDateFormat, CultureInfo.InvariantCulture),
         path,
         date
      );
   }
}

public sealed record WebStatsReport(
   string FileName,
   string Title,
   string Path,
   DateOnly? Date = null
);
