using SESport.Data.Models;

namespace SESport.Core.Tests.Pages.Admin.Dashboard;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task DashboardShowsActionableSections()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var pagePath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Dashboard/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(pagePath);

      Assert.Contains("Upcoming coverage", html);
      Assert.Contains("Activities needing attention", html);
      Assert.DoesNotContain("System health", html);
      Assert.DoesNotContain(
         "AI processing and broadcast import status.",
         html
      );
      Assert.DoesNotContain("<th>Drafts</th>", html);
      Assert.Contains(
         "admin-table admin-table-compact",
         html
      );
      Assert.Contains(
         "Broadcast imports",
         html
      );
      Assert.Contains(
         "Failed, last 25 hours",
         html
      );
      Assert.DoesNotContain(
         "Latest broadcast import",
         html
      );
      Assert.Contains(
         "<th scope=\"row\">Imported</th>",
         html
      );
      Assert.DoesNotContain(
         "<dt>Stale running</dt>",
         html
      );
      Assert.DoesNotContain(
         "<dt>Source</dt>",
         html
      );
      Assert.True(
         html.IndexOf(
            "Activities needing attention",
            StringComparison.Ordinal
         ) < html.IndexOf(
            "Upcoming coverage",
            StringComparison.Ordinal
         )
      );
      Assert.True(
         html.IndexOf(
            "Upcoming coverage",
            StringComparison.Ordinal
         ) < html.IndexOf("dashboard-health-table", StringComparison.Ordinal)
      );
      Assert.Contains("dashboard-health-table", html);
      Assert.Contains("dashboard-health-grid", html);
      Assert.Contains("dashboard-issue-pill", html);
      Assert.Contains("/Admin/Broadcasts/Index", html);
      Assert.Contains(
         "DateDisplay.DateOnlyFormat",
         html
      );
      Assert.Contains(
         "@Model.GetBroadcastDateRouteValues(",
         html
      );
      Assert.Contains("/Admin/Activities/Edit", html);
      Assert.Contains(
         "asp-route-returnUrl=\"@dashboardReturnUrl\"",
         html
      );
      Assert.DoesNotContain("bio", html, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("No direct source", html);
   }

   [Fact]
   public void GetDateRowClassUsesLighterYellowWhenVisibleBroadcastsExist()
   {
      var date = CreateDateSummary(1, 0, 1, 0);
      var rowClass =
         SESport.Web.Pages.Admin.Dashboard.IndexModel.GetDateRowClass(
            date
         );

      Assert.Equal(
         "dashboard-attention-row dashboard-attention-row-light",
         rowClass
      );
   }

   [Fact]
   public void GetDateRowClassUsesRegularYellowForOtherAttention()
   {
      var date = CreateDateSummary(1, 1, 0, 0);
      var rowClass =
         SESport.Web.Pages.Admin.Dashboard.IndexModel.GetDateRowClass(
            date
         );

      Assert.Equal("dashboard-attention-row", rowClass);
   }

   [Fact]
   public void GetDateRowClassLeavesNormalRowsUnstyled()
   {
      var date = CreateDateSummary(0, 0, 0, 0);
      var rowClass =
         SESport.Web.Pages.Admin.Dashboard.IndexModel.GetDateRowClass(
            date
         );

      Assert.Equal(string.Empty, rowClass);
   }

   [Fact]
   public void SourceIssueLabelReflectsGroupScope()
   {
      var groupedIssue = CreateSourceIssue(hasNoGroup: false);
      var ungroupedIssue = CreateSourceIssue(hasNoGroup: true);

      var groupedLabels = SESport.Web.Pages.Admin.Dashboard.IndexModel
         .GetIssueLabels(groupedIssue);
      var ungroupedLabels = SESport.Web.Pages.Admin.Dashboard.IndexModel
         .GetIssueLabels(ungroupedIssue);

      Assert.Contains("No source in group", groupedLabels);
      Assert.Contains("No source", ungroupedLabels);
      Assert.DoesNotContain("No direct source", groupedLabels);
   }

   [Fact]
   public void MissingStartTimeLabelIsIncluded()
   {
      var issue = CreateSourceIssue(
         hasNoGroup: false,
         hasMissingParticipantStartTime: true
      );

      var labels = SESport.Web.Pages.Admin.Dashboard.IndexModel
         .GetIssueLabels(issue);

      Assert.Contains("Missing participant start times", labels);
   }

   [Fact]
   public void ParticipantMissingPersonDataLabelIsIncluded()
   {
      var issue = CreateSourceIssue(
         hasNoGroup: false,
         hasParticipantMissingPersonData: true
      );

      var labels = SESport.Web.Pages.Admin.Dashboard.IndexModel
         .GetIssueLabels(issue);

      Assert.Contains(
         SESport.Web.Pages.Admin.Dashboard.IndexModel
            .ParticipantMissingPersonDataLabel,
         labels
      );
   }

   [Fact]
   public async Task ParticipantMissingPersonDataLinksToEntitiesByDate()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var pagePath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Dashboard/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(pagePath);

      Assert.Contains(
         "asp-page=\"/Admin/Entities/Index\"",
         html
      );
      Assert.Contains(
         "asp-route-date=",
         html
      );
      Assert.Contains(
         "\"@participantActivityDate\"",
         html
      );
   }

   private static DashboardDateSummary CreateDateSummary(
      int visibleBroadcastCount,
      int unreviewedBroadcastCount,
      int publishedActivityCount,
      int draftActivityCount
   ) => new(
         new DateOnly(2199, 12, 1),
         visibleBroadcastCount,
         unreviewedBroadcastCount,
         publishedActivityCount,
         draftActivityCount
      );

   private static DashboardActivityIssue CreateSourceIssue(
      bool hasNoGroup,
      bool hasMissingParticipantStartTime = false,
      bool hasParticipantMissingPersonData = false
   )
   {
      return new DashboardActivityIssue(
         Guid.NewGuid(),
         new DateOnly(2199, 12, 1),
         "Test Activity",
         "Published",
         false,
         false,
         false,
         hasNoGroup,
         true,
         hasMissingParticipantStartTime,
         hasParticipantMissingPersonData,
         new DateOnly(2199, 12, 1)
      );
   }
}
