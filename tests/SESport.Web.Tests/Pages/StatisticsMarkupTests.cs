namespace SESport.Core.Tests.Pages;

public sealed class StatisticsMarkupTests
{
   [Fact]
   public async Task StatisticsPageUsesPublicMonthDropdownAndLeaderTable()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Statistics.cshtml"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/public.css"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var css = await File.ReadAllTextAsync(cssPath);

      Assert.Contains("@page \"/statistik\"", html);
      Assert.Contains(
         "aria-label=\"Månadens mest aktiva\"",
         html
      );
      Assert.DoesNotContain("statistics-page-title", html);
      Assert.DoesNotContain("<h1", html);
      Assert.DoesNotContain("<h2", html);
      Assert.DoesNotContain(
         "<p class=\"eyebrow\">Statistik</p>",
         html
      );
      Assert.DoesNotContain("En poäng per person", html);
      Assert.DoesNotContain("Varje utövare får högst", html);
      Assert.Contains("MÅNADENS MEST AKTIVA AV TOTALT", html);
      Assert.Contains("Dagar aktiv", html);
      Assert.DoesNotContain(
         "<span class=\"date-option-day\">Månad</span>",
         html
      );
      Assert.Contains("data-date-dropdown", html);
      Assert.Contains("data-date-dropdown-toggle", html);
      Assert.Contains("data-date-dropdown-menu", html);
      Assert.Contains("asp-route-month=\"@month.Value\"", html);
      Assert.Contains("Model.SportOptions", html);
      Assert.Contains("Model.SelectedSportLabel", html);
      Assert.Contains("@Model.SelectedSportLabel:", html);
      Assert.Contains("Svenskar:", html);
      Assert.Contains(
         "asp-route-sport=\"@sport.SportId\"",
         html
      );
      Assert.Contains("data-sport-dropdown", html);
      Assert.Contains("data-sport-dropdown-toggle", html);
      Assert.Contains("data-sport-dropdown-menu", html);
      Assert.Contains("Model.Statistics.Leaders", html);
      Assert.Contains("leader.Rank", html);
      Assert.Contains("leader.Points", html);
      Assert.Contains("public-date-select.js", html);
      Assert.Contains("public-sport-select.js", html);
      Assert.Contains(".statistics-heading {", css);
      Assert.Contains(".statistics-heading-controls {", css);
      Assert.Contains(
         ".statistics-heading .statistics-sport-select {",
         css
      );
      Assert.Contains(
         "flex-direction: row;\n" +
            "      width: 100%;",
         css
      );
      Assert.Contains(
         "flex: 1 1 0;\n" +
            "      width: auto;\n" +
            "      min-width: 140px;",
         css
      );
      Assert.Contains(".statistics-table {", css);
      Assert.Contains(
         ".statistics-table-wrap {\n" +
            "   overflow-x: auto;\n" +
            "   border-radius: 12px;\n" +
            "   background: #eef5fb;\n" +
            "   color: #006aa8;\n" +
            "}",
         css
      );
      Assert.Contains(
         ".statistics-table thead th:last-child {\n" +
            "   text-align: right;\n" +
            "   white-space: nowrap;\n" +
            "}",
         css
      );
      Assert.Contains(
         ".statistics-table td.statistics-points {\n" +
            "   font-size: 16px;\n" +
            "   text-align: right;\n" +
            "}",
         css
      );
      Assert.DoesNotContain("nth-child", css);
   }
}
