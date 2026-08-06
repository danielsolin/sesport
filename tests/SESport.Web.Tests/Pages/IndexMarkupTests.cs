namespace SESport.Core.Tests.Pages;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task IndexPageIncludesParticipantsCount()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Index.cshtml"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/public.css"
      );
      var participantScriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/public-participant-table.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var css = await File.ReadAllTextAsync(cssPath);
      var participantScript = await File.ReadAllTextAsync(
         participantScriptPath
      );

      Assert.Contains("index-participants-info", html);
      Assert.Contains("aria-label=\"Visa alla sporter\"", html);
      Assert.Contains("TotalParticipantsCount", html);
      Assert.Contains(
         "const int MaxVisibleParticipants = 10;",
         html
      );
      Assert.Contains("Svenskar:", html);
      Assert.Contains("SportParticipantCounts", html);
      Assert.Contains(
         "aria-label=\"Visa endast @sport.SportName\"",
         html
      );
      Assert.Contains("index-participants-filter", html);
      Assert.Contains("is-selected", html);
      Assert.Contains("asp-route-sport=\"@sport.SportId\"", html);
      Assert.Contains("data-date-dropdown", html);
      Assert.Contains("data-date-dropdown-toggle", html);
      Assert.Contains("data-date-dropdown-menu", html);
      Assert.Contains("public-date-select.js", html);
      Assert.Contains("public-current-marker-scroll.js", html);
      Assert.Contains(
         "<script src=\"~/js/public-current-marker-scroll.js\"",
         html
      );
      Assert.DoesNotContain(
         "@*\n   <script src=\"~/js/public-current-marker-scroll.js\"",
         html
      );
      Assert.DoesNotContain("date-select-input", html);
      Assert.Contains("activity-participant-col-name", html);
      Assert.Contains("activity-participant-col-start-time", html);
      Assert.Contains(
         "data-participant-sort=\n" +
         "                                                     \"start-time\"",
         html
      );
      Assert.Contains("activity-participant-col-age", html);
      Assert.DoesNotContain("activity-participant-col-height", html);
      Assert.Contains("activity-participant-col-country", html);
      Assert.Contains("showStartTimeColumn", html);
      Assert.Contains("activity-participant-table-collapsed", html);
      Assert.Contains("activity-participant-table-frame", html);
      Assert.Contains("activity-participant-table-fade", html);
      Assert.Contains("data-participant-toggle", html);
      Assert.Contains("data-collapsed-label=\"Visa alla\"", html);
      Assert.Contains("data-expanded-label=\"Visa färre\"", html);
      Assert.Contains("activity-participant-toggle", html);
      Assert.Contains("participant.StartTime", html);
      Assert.Contains(".FormatExactTimeText(", html);
      Assert.DoesNotContain("PublicTimeDisplay.FormatTimeText(", html);
      Assert.DoesNotContain("PublicTimeDisplay.WithoutApproximation(", html);
      Assert.Contains("data-participant-start-time", html);
      Assert.True(
         html.IndexOf("activity-participant-col-name") <
         html.IndexOf("activity-participant-col-start-time")
      );
      Assert.True(
         html.IndexOf("activity-participant-col-start-time") <
         html.IndexOf("activity-participant-col-age")
      );
      Assert.Contains("activity-participant-out-badge", html);
      Assert.Contains(
         ".activity-participant-out-badge {\n" +
         "   display: inline-flex;",
         css
      );
      Assert.Contains(
         ".activity-participant-col-start-time {\n" +
         "   width: 1%;\n" +
         "   white-space: nowrap;",
         css
      );
      Assert.Contains(
         ".activity-participant-table-collapsed\n" +
         "   .activity-participant-row-collapsed {\n" +
         "   display: none;",
         css
      );
      Assert.Contains(
         ".activity-participant-table-fade {\n" +
         "   display: none;\n" +
         "   position: absolute;",
         css
      );
      Assert.Contains(
         ".activity-participant-table-frame {\n" +
         "   position: relative;\n" +
         "   width: 100%;\n" +
         "   overflow-x: auto;",
         css
      );
      Assert.Contains(
         ".activity-participant-toggle {\n" +
         "   position: relative;\n" +
         "   display: inline-block;\n" +
         "   padding: 0;\n" +
         "   border: 0;",
         css
      );
      Assert.Contains(
         ".activity-participant-toggle[aria-expanded=\"false\"] {\n" +
         "   transform: translateY(-32px);\n" +
         "   margin-bottom: -24px;",
         css
      );
      Assert.DoesNotContain(
         ".activity-has-ended .activity-participant-out-badge",
         css
      );
      Assert.DoesNotContain(
         "sortTable(table, \"name\", \"ascending\");",
         participantScript
      );
      Assert.DoesNotContain(
         "sortTable(table, \"start-time\", \"ascending\");",
         participantScript
      );
      Assert.Contains(
         "table.dataset.participantSortKey = \"start-time\";",
         participantScript
      );
      Assert.Contains(
         "updateSortHeaders(table, \"start-time\", \"ascending\");",
         participantScript
      );
      Assert.Contains("case \"start-time\":", participantScript);
      Assert.Contains("activity-now-marker", html);
      Assert.Contains("activity-ongoing-dots", html);
      Assert.Contains(
         ".activity-is-ongoing .activity-entry {\n" +
         "   border-color: #d8ad00;\n" +
         "   box-shadow: 0 0 0 3px rgba(255, 204, 0, 0.18);",
         css
      );
      Assert.Contains(
         ".activity-entry-organization {\n" +
         "   margin-top: 0px;\n" +
         "   margin-bottom: 0px;",
         css
      );
      Assert.Contains(
         ".activity-entry-title {\n" +
         "   position: relative;\n" +
         "   margin-top: 4px;\n" +
         "   margin-bottom: 0px;",
         css
      );
      Assert.DoesNotContain(
         ".activity-group-time-item.is-ongoing .activity-time-badge",
         css
      );
      Assert.DoesNotContain("activity-group-slot-status", html);
      Assert.DoesNotContain("activity-group-slot-status", css);
      Assert.Contains(
         "@keyframes activity-ongoing-dots",
         css
      );
      Assert.Contains(
         "@media (prefers-reduced-motion: reduce)",
         css
      );
      Assert.Contains("@media (orientation: portrait)", css);
      Assert.Contains(
         ".activity-participant-table-has-start-time\n" +
         "      .activity-participant-col-age {\n" +
         "      display: none;",
         css
      );
      Assert.Contains(
         ".activity-now-marker {\n      display: none;",
         css
      );
      Assert.DoesNotContain(
         ".activity-now-marker-line:first-child",
         css
      );
      Assert.DoesNotContain(
         ".activity-now-marker-line:last-child",
         css
      );
      Assert.Contains(
         ".activity-now-marker-badge {\n      padding: 5px 10px;",
         css
      );
      Assert.Contains(
         ".activity-is-ongoing .activity-entry,\n" +
         "   .activity-has-ended .activity-entry {\n" +
         "      padding-top: 52px;",
         css
      );
      Assert.Contains(
         ".activity-status-badge {\n      right: auto;\n" +
         "      left: 14px;",
         css
      );
      Assert.Contains(
         ".activity-entry-sport-icon {\n      right: 14px;\n" +
         "      left: auto;",
         css
      );
      Assert.Contains(
         ".activity-entry-sport-icon {\n   display: inline-block;\n" +
         "   position: absolute;\n   top: 13px;\n" +
         "   right: 14px;\n   width: 42px;\n" +
         "   height: 42px;",
         css
      );
      Assert.Contains("@media (orientation: landscape)", css);
      Assert.Contains(
         ".sport-select {\n" +
         "      flex: 0 0 220px;\n" +
         "      width: 220px;\n" +
         "      min-width: 220px;",
         css
      );
      Assert.Contains(
         ".activity-status-badge {\n      top: 18px;\n" +
         "      right: 68px;",
         css
      );
      Assert.Contains(
         "max-width: none;\n      padding-right: 0;\n" +
         "      overflow: visible;",
         css
      );
      Assert.Contains(
         "@media (max-width: 600px) and (orientation: portrait)",
         css
      );
      Assert.DoesNotContain(
         ".index-participants-filter.is-selected::before",
         css
      );
      Assert.Contains(
         ".index-participants-filter.is-selected::after",
         css
      );
      Assert.Contains("bottom: -9px;", css);
      Assert.DoesNotContain(
         ".activity-has-ended {\n   opacity: 0.5;",
         css
      );
      Assert.Contains(
         ".activity-has-ended {\n   opacity: 0.7;",
         css
      );
      Assert.Contains(
         ".activity-has-ended .activity-entry",
         css
      );
      Assert.Contains(
         "border-color: rgba(0, 106, 168, 0.5);",
         css
      );
      Assert.Contains(
         "--activity-duration-color: #7d8589;",
         css
      );
      Assert.Contains(
         "box-shadow: 0 0 2px rgba(125, 133, 137, 0.1);",
         css
      );
      Assert.Contains(
         ".activity-time-point {\n   display: block;\n" +
         "   flex: 0 0 18px;\n   position: relative;\n" +
         "   z-index: 3;",
         css
      );
      Assert.Contains(
         ".activity-time-row {\n   display: flex;\n" +
         "   align-items: center;\n   position: relative;\n" +
         "   z-index: 3;",
         css
      );
   }
}
